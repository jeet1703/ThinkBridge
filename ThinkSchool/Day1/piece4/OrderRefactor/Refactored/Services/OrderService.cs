using Microsoft.Extensions.Logging;
using OrderRefactor.Refactored.DTOs;
using OrderRefactor.Refactored.Models;
using OrderRefactor.Refactored.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;
        private readonly ITaxService _taxService;
        private readonly IEmailService _emailService;
        private readonly IAnalyticsService _analyticsService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository repository,
            ITaxService taxService,
            IEmailService emailService,
            IAnalyticsService analyticsService,
            IHttpClientFactory clientFactory,
            ILogger<OrderService> logger)
        {
            _repository = repository;
            _taxService = taxService;
            _emailService = emailService;
            _analyticsService = analyticsService;
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            await _analyticsService.TrackOrderAttemptAsync("new_order_attempt", cancellationToken);

            var customer = await _repository.GetCustomerByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
            {
                throw new ArgumentException("Customer not found.");
            }

            if (customer.Status == "Suspended")
            {
                throw new InvalidOperationException("This customer account has been suspended.");
            }

            decimal exchangeRate = 1.0m;
            try
            {
                var rateClient = _clientFactory.CreateClient("RateClient");
                var rateRes = await rateClient.GetAsync("http://rates.company.local/usd", cancellationToken);
                if (rateRes.IsSuccessStatusCode)
                {
                    var rateText = await rateRes.Content.ReadAsStringAsync(cancellationToken);
                    exchangeRate = decimal.Parse(rateText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch exchange rate, falling back to default.");
            }

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _repository.GetProductsByIdsAsync(productIds, cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var itemDto in request.Items)
            {
                if (itemDto.Quantity <= 0)
                {
                    throw new ArgumentException("Item quantity must be greater than zero.");
                }

                if (!productMap.TryGetValue(itemDto.ProductId, out var product))
                {
                    throw new ArgumentException($"Product with ID {itemDto.ProductId} does not exist.");
                }

                if (!product.IsActive)
                {
                    throw new InvalidOperationException($"Product '{product.Name}' is no longer active.");
                }

                if (product.StockQuantity < itemDto.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'. Only {product.StockQuantity} left.");
                }

                subtotal += product.Price * itemDto.Quantity;
                orderItems.Add(new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price
                });
            }

            var costs = _taxService.CalculateCosts(subtotal, request.Country, request.State);

            if (customer.CreditLimit < costs.Total)
            {
                throw new InvalidOperationException("Customer credit limit exceeded.");
            }

            foreach (var itemDto in request.Items)
            {
                var product = productMap[itemDto.ProductId];
                product.StockQuantity -= itemDto.Quantity;
            }

            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderDate = DateTime.Now,
                TotalAmount = costs.Total,
                TaxAmount = costs.Tax,
                ShippingCost = costs.Shipping,
                DiscountAmount = costs.Discount,
                Status = "Created",
                ShippingAddress = request.ShippingAddress,
                OrderItems = orderItems
            };

            await _repository.UpdateProductsAsync(products, cancellationToken);
            await _repository.SaveOrderAsync(order, cancellationToken);

            await _emailService.SendOrderConfirmationAsync(customer.Email, order.Id, costs.Total, cancellationToken);

            return new OrderResponse(
                Success: true,
                OrderId: order.Id,
                Total: order.TotalAmount,
                Discount: order.DiscountAmount,
                Tax: order.TaxAmount,
                Shipping: order.ShippingCost,
                ItemsCount: order.OrderItems.Count,
                ExchangeRate: exchangeRate
            );
        }
    }
}

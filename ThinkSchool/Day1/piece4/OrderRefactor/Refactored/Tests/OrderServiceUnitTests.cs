using Moq;
using OrderRefactor.Refactored.DTOs;
using OrderRefactor.Refactored.Models;
using OrderRefactor.Refactored.Repositories;
using OrderRefactor.Refactored.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderRefactor.Refactored.Tests
{
    public class OrderServiceUnitTests
    {
        private readonly Mock<IOrderRepository> _repoMock;
        private readonly Mock<ITaxService> _taxMock;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<IAnalyticsService> _analyticsMock;
        private readonly Mock<IHttpClientFactory> _clientFactoryMock;
        private readonly Mock<Microsoft.Extensions.Logging.ILogger<OrderService>> _loggerMock;
        private readonly OrderService _orderService;

        public OrderServiceUnitTests()
        {
            _repoMock = new Mock<IOrderRepository>();
            _taxMock = new Mock<ITaxService>();
            _emailMock = new Mock<IEmailService>();
            _analyticsMock = new Mock<IAnalyticsService>();
            _clientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<OrderService>>();

            _orderService = new OrderService(
                _repoMock.Object,
                _taxMock.Object,
                _emailMock.Object,
                _analyticsMock.Object,
                _clientFactoryMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task CreateOrder_Throws_WhenCustomerNotFound()
        {
            _repoMock.Setup(r => r.GetCustomerByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Customer?)null);

            var request = new CreateOrderRequest(999, "Address", "NY", "US", new List<OrderItemDto>());

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _orderService.CreateOrderAsync(request, CancellationToken.None));
            Assert.Equal("Customer not found.", ex.Message);
        }

        [Fact]
        public async Task CreateOrder_Throws_WhenCustomerSuspended()
        {
            var customer = new Customer { Id = 1, Status = "Suspended" };
            _repoMock.Setup(r => r.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var request = new CreateOrderRequest(1, "Address", "NY", "US", new List<OrderItemDto>());

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.CreateOrderAsync(request, CancellationToken.None));
            Assert.Equal("This customer account has been suspended.", ex.Message);
        }

        [Fact]
        public async Task CreateOrder_Throws_WhenCreditLimitExceeded()
        {
            var customer = new Customer { Id = 1, Status = "Active", CreditLimit = 50.00m };
            _repoMock.Setup(r => r.GetCustomerByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(customer);

            var products = new List<Product>
            {
                new Product { Id = 10, Price = 100.00m, IsActive = true, StockQuantity = 5 }
            };
            _repoMock.Setup(r => r.GetProductsByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            var request = new CreateOrderRequest(1, "Address", "NY", "US", new List<OrderItemDto>
            {
                new OrderItemDto(10, 1)
            });

            _taxMock.Setup(t => t.CalculateCosts(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new OrderCosts(0, 0, 0, 100.00m));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _orderService.CreateOrderAsync(request, CancellationToken.None));
            Assert.Equal("Customer credit limit exceeded.", ex.Message);
        }
    }
}

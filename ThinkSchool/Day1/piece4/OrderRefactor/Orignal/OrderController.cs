using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;

namespace OrderRefactor.Orignal
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(AppDbContext context, ILogger<OrderController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] object requestBody)
        {
            // Try sending analytics log to legacy HTTP collector
            try
            {
                // Empty catch block #1: Analytics tracking failure shouldn't stop checkout
                var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(200) };
                var response = client.PostAsync("http://internal-analytics.local/track", new StringContent("new_order_attempt")).Result;
            }
            catch (Exception)
            {
                // Legacy system is unstable, ignore errors
            }

            if (requestBody == null)
            {
                return BadRequest("Request payload is empty.");
            }

            // Parse request elements manually since we receive object type
            Dictionary<string, object> bodyDict;
            try
            {
                bodyDict = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBody.ToString());
            }
            catch (Exception)
            {
                return BadRequest("Malformed JSON payload.");
            }

            // Extract customer info and shipping details
            int customerId = int.Parse(bodyDict["customerId"].ToString());
            string country = bodyDict["country"].ToString();
            string state = bodyDict["state"].ToString();
            string address = bodyDict["shippingAddress"].ToString();
            
            // Extract items array
            var itemsJson = JsonSerializer.Deserialize<List<Dictionary<string, int>>>(bodyDict["items"].ToString());

            // Validate fields are not empty
            if (string.IsNullOrEmpty(country) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(address))
            {
                return BadRequest("Shipping details are required.");
            }

            // Synchronous EF Core call inside an async method
            var customer = _context.Customers.FirstOrDefault(c => c.Id == customerId);

            // Null reference bug: customer can be null if not found in database
            // Dereferencing customer.Status will throw NullReferenceException
            if (customer.Status == "Suspended")
            {
                return BadRequest("This customer account has been suspended.");
            }

            // Dynamic exchange rate lookup from legacy api
            decimal exchangeRate = 1.0m;
            try
            {
                // Empty catch block #2: Call external service, fallback if service is down
                var rateClient = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
                var rateRes = rateClient.GetAsync("http://rates.company.local/usd").Result;
                exchangeRate = decimal.Parse(rateRes.Content.ReadAsStringAsync().Result);
            }
            catch (Exception)
            {
                // Just use 1.0 default exchange rate
            }

            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            try
            {
                // Off-by-one bug: i <= itemsJson.Count instead of i < itemsJson.Count
                // IndexOutOfRangeException is thrown on final iteration
                for (int i = 0; i <= itemsJson.Count; i++)
                {
                    var item = itemsJson[i];
                    int prodId = item["productId"];
                    int qty = item["quantity"];

                    if (qty <= 0)
                    {
                        return BadRequest("Item quantity must be greater than zero.");
                    }

                    // Synchronous EF Core query in loops (N+1 query pattern)
                    var product = _context.Products.FirstOrDefault(p => p.Id == prodId);
                    if (product == null)
                    {
                        return BadRequest($"Product with ID {prodId} does not exist.");
                    }

                    if (!product.IsActive)
                    {
                        return BadRequest($"Product '{product.Name}' is no longer active.");
                    }

                    if (product.StockQuantity < qty)
                    {
                        return BadRequest($"Insufficient stock for product '{product.Name}'. Only {product.StockQuantity} left.");
                    }

                    // Deduct stock immediately (sync update)
                    product.StockQuantity -= qty;
                    _context.SaveChanges(); 

                    subtotal += product.Price * qty;
                    orderItems.Add(new OrderItem
                    {
                        ProductId = prodId,
                        Quantity = qty,
                        UnitPrice = product.Price
                    });
                }
            }
            catch (Exception)
            {
                // Empty catch block #3: Absorbs the IndexOutOfRangeException from the off-by-one loop bug.
                // The order process continues but the last item in the request is silently ignored.
            }

            if (orderItems.Count == 0)
            {
                return BadRequest("Order must contain at least one valid item.");
            }

            // Duplicated & Hardcoded business logic: calculate tax & shipping & discount (Block 1)
            decimal discount = 0.0m;
            if (subtotal > 150.0m)
            {
                discount = subtotal * 0.10m; // 10% discount above 150
            }
            else if (subtotal > 75.0m)
            {
                discount = subtotal * 0.05m; // 5% discount above 75
            }

            decimal discountedSubtotal = subtotal - discount;

            decimal tax = 0.0m;
            if (country == "US")
            {
                tax = state == "NY" ? discountedSubtotal * 0.08875m : discountedSubtotal * 0.05m;
            }
            else if (country == "CA")
            {
                tax = discountedSubtotal * 0.12m;
            }

            decimal shipping = discountedSubtotal < 50.0m ? 9.99m : 0.0m;
            decimal estimatedTotal = discountedSubtotal + tax + shipping;

            // Credit validation
            if (customer.CreditLimit < estimatedTotal)
            {
                // Rollback stock updates if customer cannot afford the order
                foreach (var oi in orderItems)
                {
                    var p = _context.Products.FirstOrDefault(prod => prod.Id == oi.ProductId);
                    if (p != null)
                    {
                        p.StockQuantity += oi.Quantity;
                        _context.SaveChanges();
                    }
                }
                return BadRequest("Customer credit limit exceeded.");
            }

            // Duplicated & Hardcoded business logic: calculate tax & shipping & discount (Block 2)
            decimal finalDiscount = 0.0m;
            if (subtotal > 150.0m)
            {
                finalDiscount = subtotal * 0.10m;
            }
            else if (subtotal > 75.0m)
            {
                finalDiscount = subtotal * 0.05m;
            }

            decimal finalDiscountedSubtotal = subtotal - finalDiscount;

            decimal finalTax = 0.0m;
            if (country == "US")
            {
                finalTax = state == "NY" ? finalDiscountedSubtotal * 0.08875m : finalDiscountedSubtotal * 0.05m;
            }
            else if (country == "CA")
            {
                finalTax = finalDiscountedSubtotal * 0.12m;
            }

            decimal finalShipping = finalDiscountedSubtotal < 50.0m ? 9.99m : 0.0m;
            decimal finalTotal = finalDiscountedSubtotal + finalTax + finalShipping;

            var order = new Order
            {
                CustomerId = customerId,
                OrderDate = DateTime.Now,
                TotalAmount = finalTotal,
                TaxAmount = finalTax,
                ShippingCost = finalShipping,
                DiscountAmount = finalDiscount,
                Status = "Created",
                ShippingAddress = address,
                OrderItems = orderItems
            };

            // Save order to db (sync call)
            _context.Orders.Add(order);
            _context.SaveChanges();

            // Try sending confirmation email
            try
            {
                // Empty catch block #4: If mail server is offline, order is still placed
                var smtp = new System.Net.Mail.SmtpClient("mail-relay.company.local");
                smtp.Send("orders@shop.local", customer.Email, "Order Confirmation", $"Order {order.Id} placed. Total: {finalTotal}");
            }
            catch (Exception)
            {
                // Silently skip email notification failures
            }

            // Return untyped anonymous response object instead of proper DTO
            return Ok(new
            {
                success = true,
                orderId = order.Id,
                total = order.TotalAmount,
                discount = order.DiscountAmount,
                tax = order.TaxAmount,
                shipping = order.ShippingCost,
                itemsCount = order.OrderItems.Count,
                exchangeRate = exchangeRate
            });
        }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public string Status { get; set; }
        public string ShippingAddress { get; set; }
        public List<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string Country { get; set; }
        public string State { get; set; }
        public decimal CreditLimit { get; set; }
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
    }
}

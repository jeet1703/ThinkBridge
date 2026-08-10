using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using OrderRefactor.Refactored.DTOs;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace OrderRefactor.Refactored.Tests
{
    public class OrderControllerIntegrationTests
    {
        private class CustomApiFactory : WebApplicationFactory<Program>
        {
            private readonly string _controllerNamespace;

            public CustomApiFactory(string controllerNamespace)
            {
                _controllerNamespace = controllerNamespace;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseSetting("ControllerNamespace", _controllerNamespace);
            }
        }

        [Fact]
        public async Task OriginalController_ThrowsNullReference_WhenCustomerNotFound()
        {
            using var factory = new CustomApiFactory("OrderRefactor.Orignal");
            var client = factory.CreateClient();

            var payload = new
            {
                customerId = 999,
                shippingAddress = "123 Main St",
                state = "NY",
                country = "US",
                items = new[] { new { productId = 1, quantity = 2 } }
            };

            var response = await client.PostAsJsonAsync("api/orders", payload);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task RefactoredController_ReturnsBadRequest_WhenCustomerNotFound()
        {
            using var factory = new CustomApiFactory("OrderRefactor.Refactored");
            var client = factory.CreateClient();

            var payload = new CreateOrderRequest(
                CustomerId: 999,
                ShippingAddress: "123 Main St",
                State: "NY",
                Country: "US",
                Items: new List<OrderItemDto> { new OrderItemDto(1, 2) }
            );

            var response = await client.PostAsJsonAsync("api/orders", payload);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Customer not found.", content);
        }
    }
}

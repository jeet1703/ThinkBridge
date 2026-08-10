using Microsoft.AspNetCore.Mvc;
using OrderRefactor.Refactored.DTOs;
using OrderRefactor.Refactored.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderRefactor.Refactored.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder(
            [FromBody] CreateOrderRequest request, 
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _orderService.CreateOrderAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

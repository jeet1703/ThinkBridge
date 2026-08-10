using System.Collections.Generic;

namespace OrderRefactor.Refactored.DTOs
{
    public record CreateOrderRequest(
        int CustomerId,
        string ShippingAddress,
        string State,
        string Country,
        List<OrderItemDto> Items
    );
}

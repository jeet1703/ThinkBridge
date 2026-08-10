namespace OrderRefactor.Refactored.DTOs
{
    public record OrderResponse(
        bool Success,
        int OrderId,
        decimal Total,
        decimal Discount,
        decimal Tax,
        decimal Shipping,
        int ItemsCount,
        decimal ExchangeRate
    );
}

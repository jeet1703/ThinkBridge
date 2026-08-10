namespace OrderRefactor.Refactored.Services
{
    public record OrderCosts(decimal Discount, decimal Tax, decimal Shipping, decimal Total);

    public interface ITaxService
    {
        OrderCosts CalculateCosts(decimal subtotal, string country, string state);
    }
}

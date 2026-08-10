namespace OrderRefactor.Refactored.Services
{
    public interface IPricingStrategy
    {
        bool AppliesTo(string country);
        OrderCosts CalculateCosts(decimal subtotal, string state);
    }
}

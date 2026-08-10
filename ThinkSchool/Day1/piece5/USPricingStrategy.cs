using System;

namespace OrderRefactor.Refactored.Services
{
    public class USPricingStrategy : IPricingStrategy
    {
        public bool AppliesTo(string country)
        {
            return string.Equals(country, "US", StringComparison.OrdinalIgnoreCase);
        }

        public OrderCosts CalculateCosts(decimal subtotal, string state)
        {
            decimal discount = 0.0m;
            if (subtotal > 150.0m)
            {
                discount = subtotal * 0.10m;
            }
            else if (subtotal > 75.0m)
            {
                discount = subtotal * 0.05m;
            }

            decimal discountedSubtotal = subtotal - discount;

            // NY state has 8.875% tax, other states have 5% tax
            decimal tax = string.Equals(state, "NY", StringComparison.OrdinalIgnoreCase) 
                ? discountedSubtotal * 0.08875m 
                : discountedSubtotal * 0.05m;

            decimal shipping = discountedSubtotal < 50.0m ? 9.99m : 0.0m;
            decimal total = discountedSubtotal + tax + shipping;

            return new OrderCosts(discount, tax, shipping, total);
        }
    }
}

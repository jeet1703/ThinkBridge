using System;

namespace OrderRefactor.Refactored.Services
{
    public class CanadaPricingStrategy : IPricingStrategy
    {
        public bool AppliesTo(string country)
        {
            return string.Equals(country, "CA", StringComparison.OrdinalIgnoreCase);
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

            // Canada has a flat 12% tax
            decimal tax = discountedSubtotal * 0.12m;

            decimal shipping = discountedSubtotal < 50.0m ? 9.99m : 0.0m;
            decimal total = discountedSubtotal + tax + shipping;

            return new OrderCosts(discount, tax, shipping, total);
        }
    }
}

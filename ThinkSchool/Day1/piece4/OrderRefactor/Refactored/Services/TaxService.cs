namespace OrderRefactor.Refactored.Services
{
    public class TaxService : ITaxService
    {
        public OrderCosts CalculateCosts(decimal subtotal, string country, string state)
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
            decimal total = discountedSubtotal + tax + shipping;

            return new OrderCosts(discount, tax, shipping, total);
        }
    }
}

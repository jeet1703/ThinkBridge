using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderRefactor.Refactored.Services
{
    public class TaxService : ITaxService
    {
        private readonly IEnumerable<IPricingStrategy> _strategies;

        public TaxService(IEnumerable<IPricingStrategy> strategies)
        {
            _strategies = strategies;
        }

        public OrderCosts CalculateCosts(decimal subtotal, string country, string state)
        {
            var strategy = _strategies.FirstOrDefault(s => s.AppliesTo(country));
            if (strategy == null)
            {
                throw new NotSupportedException($"Pricing strategy for country '{country}' is not supported.");
            }
            return strategy.CalculateCosts(subtotal, state);
        }
    }
}

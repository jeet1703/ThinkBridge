# AI Reflection

### What did Claude Code get right?

Claude correctly identified the tax and shipping calculations as a good place to use the Strategy Pattern. It moved the country-specific logic into separate strategies like `USPricingStrategy` and `CanadaPricingStrategy` without changing the existing `ITaxService` contract. I also pushed back on changing the validation logic because it was tied to the database and the order flow.

### Where would you have caught a bug?

I would have caught the issue while reviewing the tests. The mock for `GetProductsByIdsAsync` was missing, so the products list was null and `CreateOrderAsync` failed at `products.ToDictionary()` before it even reached the validation I was trying to test. Running the tests and reading the failure made the issue fairly easy to spot.

### What did Copilot save / suggest wrong?

Copilot saved some time by generating the basic test setup, mocks, and request data. However, one of its suggestions didn't account for the repository calls properly. Because of that, the test threw an `ArgumentNullException` instead of the `ArgumentException` I was expecting for a negative quantity.

### What would I use at 2 AM?

I'd probably use Claude Code first for a production issue. If the problem involved multiple files, configuration, or database-related code, being able to look across the project would make debugging easier. I'd still verify everything myself instead of blindly applying its suggestions.

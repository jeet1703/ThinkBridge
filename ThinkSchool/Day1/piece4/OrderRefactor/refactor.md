Refactoring Notes

Smells spotted in the old OrderController:

1. God method - The controller does literally everything: parses json, does credit checks, inventory deduction, SMTP email dispatch, and database writes. Needs to be split up.
2. Sync DB calls in async method - Using FirstOrDefault and SaveChanges. This blocks threads under heavy traffic. Needs to be FirstOrDefaultAsync and SaveChangesAsync.
3. Swallowing exceptions - It has 4 empty catch blocks. If the analytics, exchange rate API, mail server, or even the main logic crashes, we'll never know because it fails silently.
4. Raw object request/response - Endpoint takes a generic object and returns anonymous types. Swagger can't generate documentation, and we have to manually deserialize properties which is super error-prone.
5. Injected DbContext - Injected the AppDbContext straight into the controller. Highly coupled, can't unit test without a database.
6. Copied logic - The shipping, tax, and discount math is duplicated twice (once for the estimated check, once for the final save). If a tax rate changes, we have to remember to update both.
7. Database query in loop - Loading products one by one in the loop. That's a classic N+1 query issue. We should load them in one bulk query before the loop.
8. Off-by-one error - The loop goes to i <= itemsJson.Count. This throws IndexOutOfRangeException on the last item. And because of the empty catch, it silently drops the last item from the checkout.
9. Null ref risk - Dereferencing customer.Status right after loading the customer without checking if it's null. If the ID is bad, the API crashes with a 500 error.
10. Directly instantiating clients - Newing up HttpClient and SmtpClient inline. Bad for socket management and makes testing external integrations impossible.
11. Modifying DB state too early - Decreasing stock and calling SaveChanges in the loop before verifying credit limit. If credit check fails, we have to run messy rollback code.

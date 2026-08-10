I need a deliberately bad legacy OrderController.cs for an ASP.NET Core 10 API that I can use for a refactoring exercise.

Make it around 300 lines and put most of the work inside one POST /api/orders action. I want it to mix request parsing, validation, business logic, EF Core/database calls and response creation all in the controller.

Please include a few realistic legacy-code problems:

around 4 empty catch { } blocks

synchronous EF Core calls inside an async method

object for the request/response instead of proper DTOs

direct DbContext usage

some duplicated or hard-coded business logic

one off-by-one bug

one possible null reference bug

no tests

Make the code look like something that could realistically have been written a few years ago, not like an obvious toy example.

I want the original code only. Don't refactor it, fix the bugs, or explain the problems. Just give me the OrderController.cs file.
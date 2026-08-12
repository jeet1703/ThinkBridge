using QuotesApi.Extensions;
using QuotesApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

await app.ApplyMigrationsAsync();

app.MapQuoteEndpoints();
app.MapCollectionEndpoints();
app.MapAuthEndpoints();

app.Run();

public partial class Program { }
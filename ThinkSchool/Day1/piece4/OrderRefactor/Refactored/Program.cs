using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderRefactor.Refactored.Repositories;
using OrderRefactor.Refactored.Services;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

var controllerNamespace = builder.Configuration["ControllerNamespace"] ?? "OrderRefactor.Refactored";

builder.Services.AddControllers()
    .ConfigureApplicationPartManager(pm =>
    {
        pm.FeatureProviders.Add(new ControllerNamespaceFilter(controllerNamespace));
    });

builder.Services.AddDbContext<OrderRefactor.Orignal.AppDbContext>(options =>
    options.UseInMemoryDatabase("OriginalDb"));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("RefactoredDb"));

builder.Services.AddHttpClient("AnalyticsClient");
builder.Services.AddHttpClient("RateClient");

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<ITaxService, TaxService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

app.MapControllers();

app.Run();

public class ControllerNamespaceFilter : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly string _allowedNamespace;

    public ControllerNamespaceFilter(string allowedNamespace)
    {
        _allowedNamespace = allowedNamespace;
    }

    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        for (int i = feature.Controllers.Count - 1; i >= 0; i--)
        {
            var controller = feature.Controllers[i];
            if (controller.Namespace == null || !controller.Namespace.StartsWith(_allowedNamespace))
            {
                feature.Controllers.RemoveAt(i);
            }
        }
    }
}

public partial class Program { }

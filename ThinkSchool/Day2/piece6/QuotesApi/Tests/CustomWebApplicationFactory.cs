using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace QuotesApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(AppContext.BaseDirectory);

        var dbPath = Path.Combine(AppContext.BaseDirectory, "quotes_test.db");
        if (File.Exists(dbPath))
        {
            try
            {
                File.Delete(dbPath);
            }
            catch
            {
            }
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:QuotesDb", $"Data Source={dbPath}" }
            });
        });
    }
}

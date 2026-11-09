using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudShare.Api.Data;
using StudShare.Api.Services;

namespace StudShare.UnitTests.System;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", "super-secret-key-change-in-production-min-32-chars");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "testpass");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _connection.CreateFunction("now", () => DateTime.UtcNow);
        _connection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(EmailService));
            if (emailDescriptor != null)
                services.Remove(emailDescriptor);

            services.AddScoped<EmailService, DummyEmailService>();

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _connection.Dispose();
    }
}

public class DummyEmailService(IConfiguration config) : EmailService(config)
{
    public override Task SendAsync(string to, string subject, string htmlBody) =>
        Task.CompletedTask;
}

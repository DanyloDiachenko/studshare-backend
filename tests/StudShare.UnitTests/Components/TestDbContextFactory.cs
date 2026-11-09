using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudShare.Api.Data;

namespace StudShare.UnitTests.Components;

public static class TestDbContextFactory
{
    public static (AppDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        connection.CreateFunction("now", () => DateTime.UtcNow);
        connection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}

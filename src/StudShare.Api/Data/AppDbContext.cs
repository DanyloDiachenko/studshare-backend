using Microsoft.EntityFrameworkCore;
using StudShare.Api.Entities;

namespace StudShare.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupInvitation> GroupInvitations => Set<GroupInvitation>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<Entities.File> Files => Set<Entities.File>();
    public DbSet<HistoryEvent> HistoryEvents => Set<HistoryEvent>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

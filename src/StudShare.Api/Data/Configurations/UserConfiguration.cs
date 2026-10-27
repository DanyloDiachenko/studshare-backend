using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudShare.Api.Entities;

namespace StudShare.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(u => u.Id);

        e.Property(u => u.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        e.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        e.HasIndex(u => u.Email)
            .IsUnique();

        e.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        e.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        e.Property(u => u.MiddleName)
            .HasMaxLength(100);

        e.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>();

        e.Property(u => u.GroupId);

        e.Property(u => u.UploadAccess)
            .IsRequired()
            .HasDefaultValue(false);

        e.Property(u => u.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.Property(u => u.InvitedAt);

        e.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(128);
    }
}

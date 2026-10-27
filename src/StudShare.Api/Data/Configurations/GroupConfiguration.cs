using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudShare.Api.Entities;

namespace StudShare.Api.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> e)
    {
        e.ToTable("groups");
        e.HasKey(g => g.Id);

        e.Property(g => g.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        e.Property(g => g.Name)
            .IsRequired()
            .HasMaxLength(200);

        e.Property(g => g.Faculty)
            .IsRequired()
            .HasMaxLength(200);

        e.Property(g => g.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.Property(g => g.StorageUsedBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        e.Property(g => g.StorageLimitBytes)
            .IsRequired()
            .HasDefaultValue(1_000_000_000L);

        e.Ignore(g => g.Members);
        e.Ignore(g => g.Invitations);
        e.Ignore(g => g.History);
        e.Ignore(g => g.Folders);
        e.Ignore(g => g.Files);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StudShare.Api.Data.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<Entities.File>
{
    public void Configure(EntityTypeBuilder<Entities.File> e)
    {
        e.ToTable("files");
        e.HasKey(f => f.Id);

        e.Property(f => f.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        e.Property(f => f.GroupId)
            .IsRequired();

        e.Property(f => f.FolderId);

        e.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        e.Property(f => f.Extension)
            .IsRequired()
            .HasMaxLength(20);

        e.Property(f => f.Category)
            .IsRequired()
            .HasConversion<string>();

        e.Property(f => f.SizeBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        e.Property(f => f.Url)
            .IsRequired()
            .HasMaxLength(2048);

        e.Property(f => f.IsPinned)
            .IsRequired()
            .HasDefaultValue(false);

        e.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.Property(f => f.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.OwnsOne(f => f.Author, a =>
        {
            a.Property(x => x.Id).HasColumnName("author_id").IsRequired();
            a.Property(x => x.FirstName).HasColumnName("author_first_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.LastName).HasColumnName("author_last_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.MiddleName).HasColumnName("author_middle_name").HasMaxLength(100);
            a.Property(x => x.Role).HasColumnName("author_role").HasConversion<string>().IsRequired();
        });
    }
}

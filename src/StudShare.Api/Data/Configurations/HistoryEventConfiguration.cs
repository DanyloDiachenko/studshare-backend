using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudShare.Api.Entities;

namespace StudShare.Api.Data.Configurations;

public class HistoryEventConfiguration : IEntityTypeConfiguration<HistoryEvent>
{
    public void Configure(EntityTypeBuilder<HistoryEvent> e)
    {
        e.ToTable("history_events");
        e.HasKey(h => h.Id);

        e.Property(h => h.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        e.Property(h => h.GroupId)
            .IsRequired();

        e.Property(h => h.Kind)
            .IsRequired()
            .HasConversion<string>();

        e.Property(h => h.Text)
            .IsRequired()
            .HasMaxLength(1000);

        e.Property(h => h.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.OwnsOne(h => h.Actor, a =>
        {
            a.Property(x => x.Id).HasColumnName("actor_id").IsRequired();
            a.Property(x => x.FirstName).HasColumnName("actor_first_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.LastName).HasColumnName("actor_last_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.MiddleName).HasColumnName("actor_middle_name").HasMaxLength(100);
            a.Property(x => x.Role).HasColumnName("actor_role").HasConversion<string>().IsRequired();
        });
    }
}

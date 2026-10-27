using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudShare.Api.Entities;

namespace StudShare.Api.Data.Configurations;

public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> e)
    {
        e.ToTable("group_invitations");
        e.HasKey(i => i.Id);

        e.Property(i => i.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        e.Property(i => i.GroupId)
            .IsRequired();

        e.Property(i => i.Email)
            .IsRequired()
            .HasMaxLength(256);

        e.HasIndex(i => new { i.GroupId, i.Email })
            .IsUnique();

        e.Property(i => i.InvitedAt)
            .IsRequired()
            .HasDefaultValueSql("now()");

        e.Property(i => i.ExpiresAt)
            .IsRequired();

        e.OwnsOne(i => i.InvitedBy, a =>
        {
            a.Property(x => x.Id).HasColumnName("invited_by_id").IsRequired();
            a.Property(x => x.FirstName).HasColumnName("invited_by_first_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.LastName).HasColumnName("invited_by_last_name").HasMaxLength(100).IsRequired();
            a.Property(x => x.MiddleName).HasColumnName("invited_by_middle_name").HasMaxLength(100);
            a.Property(x => x.Role).HasColumnName("invited_by_role").HasConversion<string>().IsRequired();
        });
    }
}

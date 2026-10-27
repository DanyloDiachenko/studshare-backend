using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudShare.Api.Entities;

namespace StudShare.Api.Data.Configurations;

public class EmailVerificationCodeConfiguration : IEntityTypeConfiguration<EmailVerificationCode>
{
    public void Configure(EntityTypeBuilder<EmailVerificationCode> builder)
    {
        builder.ToTable("email_verification_codes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email).HasMaxLength(256).IsRequired();
        builder.Property(e => e.Code).HasMaxLength(6).IsRequired();
        builder.Property(e => e.Purpose).HasConversion<string>().HasMaxLength(32);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(e => new { e.Email, e.Purpose });
    }
}

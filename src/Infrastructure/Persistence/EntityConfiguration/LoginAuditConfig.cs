using Domain.Aggregates.LoginAudit;
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class LoginAuditConfig : IEntityTypeConfiguration<LoginAudit>
{
    public void Configure(EntityTypeBuilder<LoginAudit> builder)
    {
        builder.ToTable("LoginAudits");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(LoginAudit.MaxEmailLength);

        builder.Property(x => x.Outcome).IsRequired();

        builder.Property(x => x.IpAddress).HasMaxLength(LoginAudit.MaxIpAddressLength);
        builder.Property(x => x.UserAgent).HasMaxLength(LoginAudit.MaxUserAgentLength);

        builder.Property(x => x.OccurredAtUtc).IsRequired();

        // Newest first, for the admin list.
        builder.HasIndex(x => x.OccurredAtUtc)
            .IsDescending();

        // "My login history" and "history of one user".
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc })
            .IsDescending(false, true);

        // "Attempts for this email", including emails that have no account.
        builder.HasIndex(x => new { x.Email, x.OccurredAtUtc })
            .IsDescending(false, true);

        // The audit row stays if the account is ever removed.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
using Domain.Aggregates.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfiguration;

public class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsEmailVerified);

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(p => p.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(256);

            email.HasIndex(p => p.Value).IsUnique();
        });
        builder.Navigation(x => x.Email).IsRequired();

        builder.OwnsOne(x => x.PasswordHash, password =>
        {
            password.Property(p => p.Value)
                .HasColumnName("PasswordHash")
                .IsRequired();
        });
        builder.Navigation(x => x.PasswordHash).IsRequired();

        builder.OwnsOne(x => x.FirstName, name =>
        {
            name.Property(p => p.Value)
                .HasColumnName("FirstName")
                .IsRequired()
                .HasMaxLength(100);
        });
        builder.Navigation(x => x.FirstName).IsRequired();

        builder.OwnsOne(x => x.LastName, name =>
        {
            name.Property(p => p.Value)
                .HasColumnName("LastName")
                .IsRequired()
                .HasMaxLength(100);
        });
        builder.Navigation(x => x.LastName).IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.VerificationToken)
            .HasMaxLength(200);

        builder.Property(x => x.RegisteredAtUtc)
            .IsRequired();
    }
}
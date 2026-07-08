// Layer: Infrastructure
// Responsibility: Configures EF Core mapping for the User entity.
using Greenlab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Greenlab.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.FullName).HasMaxLength(256);
        builder.Property(x => x.Role).HasConversion<int>();
    }
}

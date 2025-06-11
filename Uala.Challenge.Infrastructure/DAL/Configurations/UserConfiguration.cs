using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Infrastructure.DAL.Configurations
{
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users")
                .HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasColumnName("id")
                .IsRequired()
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(t => t.Username)
                .HasColumnName("username")
                .HasColumnType("text")
                .HasMaxLength(50)
                .IsRequired();

            builder.HasMany(u => u.Following)
                .WithMany()
                .UsingEntity(j => j.ToTable("UserFollowers"));
        }
    }
}

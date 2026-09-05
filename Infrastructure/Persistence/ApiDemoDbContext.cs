using Cqrs.RetailerIsolation;
using Microsoft.EntityFrameworkCore;

namespace FunctionalProgramming.Infrastructure.Persistence;

public sealed class ApiDemoDbContext(DbContextOptions<ApiDemoDbContext> options, IRetailerContext retailerContext) : DbContext(options)
{
    private Guid CurrentRetailerId => retailerContext.RetailerId;

    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var message = modelBuilder.Entity<MessageEntity>();
        message.HasKey(x => x.Id);
        message.Property(x => x.Text).HasMaxLength(2000).IsRequired();
        message.Property(x => x.AttributesJson).IsRequired();
        message.HasIndex(x => new { x.RetailerId, x.Id }).IsUnique();
        message.HasQueryFilter(x => x.RetailerId == CurrentRetailerId);
    }
}

public sealed class MessageEntity : IBelongsToRetailer
{
    public int Id { get; set; }
    public Guid RetailerId { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedUtc { get; set; }
    public required string AttributesJson { get; set; }
}

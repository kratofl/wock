using Microsoft.EntityFrameworkCore;
using Wock.Models;

namespace Wock.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<BookingTarget> BookingTargets => Set<BookingTarget>();

    public DbSet<WorkEntry> WorkEntries => Set<WorkEntry>();

    public DbSet<WorkEntryPause> WorkEntryPauses => Set<WorkEntryPause>();

    public DbSet<InstalledPlugin> InstalledPlugins => Set<InstalledPlugin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.Property(customer => customer.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(customer => customer.Notes)
                .HasMaxLength(2000);

            entity.HasIndex(customer => customer.IsActive);
        });

        modelBuilder.Entity<BookingTarget>(entity =>
        {
            entity.Property(target => target.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(target => target.BookingSoftware)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(target => target.BookingTicketId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(target => target.Notes)
                .HasMaxLength(2000);

            entity.HasOne(target => target.Customer)
                .WithMany(customer => customer.BookingTargets)
                .HasForeignKey(target => target.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(target => target.CustomerId);
            entity.HasIndex(target => target.IsActive);
        });

        modelBuilder.Entity<WorkEntry>(entity =>
        {
            entity.ToTable(table => table.HasCheckConstraint(
                "CK_WorkEntries_TotalPausedSeconds_NonNegative",
                $"{nameof(WorkEntry.TotalPausedSeconds)} >= 0"));

            entity.Property(entry => entry.ExternalTicketId)
                .HasMaxLength(100);

            entity.Property(entry => entry.Description)
                .HasMaxLength(2000);

            entity.Property(entry => entry.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            entity.HasOne(entry => entry.Customer)
                .WithMany(customer => customer.WorkEntries)
                .HasForeignKey(entry => entry.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(entry => entry.BookingTarget)
                .WithMany(target => target.WorkEntries)
                .HasForeignKey(entry => entry.BookingTargetId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(entry => entry.Pauses)
                .WithOne(pause => pause.WorkEntry)
                .HasForeignKey(pause => pause.WorkEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(entry => entry.Status);
            entity.HasIndex(entry => entry.StartedAt);
            entity.HasIndex(entry => entry.CustomerId);
            entity.HasIndex(entry => entry.BookingTargetId);
            entity.HasIndex(entry => entry.ExternalTicketId);
            entity.HasIndex(entry => new { entry.Status, entry.StartedAt });
            entity.HasIndex(entry => new { entry.CustomerId, entry.StartedAt });
        });

        modelBuilder.Entity<WorkEntryPause>(entity =>
        {
            entity.HasIndex(pause => pause.WorkEntryId);
            entity.HasIndex(pause => pause.PausedAt);
        });

        modelBuilder.Entity<InstalledPlugin>(entity =>
        {
            entity.Property(plugin => plugin.PluginId)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(plugin => plugin.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(plugin => plugin.Version)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(plugin => plugin.Description)
                .HasMaxLength(2000);

            entity.Property(plugin => plugin.AssemblyPath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(plugin => plugin.TypeName)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(plugin => plugin.LastLoadStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(PluginLoadStatus.NotLoaded);

            entity.Property(plugin => plugin.LastLoadError)
                .HasMaxLength(4000);

            entity.Property(plugin => plugin.IsEnabled)
                .HasDefaultValue(false);

            entity.HasIndex(plugin => plugin.PluginId)
                .IsUnique();

            entity.HasIndex(plugin => plugin.IsEnabled);
            entity.HasIndex(plugin => plugin.LastLoadStatus);
        });
    }
}

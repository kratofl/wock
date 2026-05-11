using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wock.Common.Domain;
using Wock.Common.Security;
using Wock.Common.Time;
using Wock.Features.Users.Models;
using Wock.Models;

namespace Wock.Data;

public class AppDbContext : DbContext
{
    private const int UserIdMaxLength = 450;

    private readonly ICurrentUserContext _currentUserContext;
    private readonly ISystemClock _clock;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserContext currentUserContext,
        ISystemClock clock)
        : base(options)
    {
        _currentUserContext = currentUserContext;
        _clock = clock;
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<BookingTarget> BookingTargets => Set<BookingTarget>();

    public DbSet<WorkEntry> WorkEntries => Set<WorkEntry>();

    public DbSet<WorkEntryPause> WorkEntryPauses => Set<WorkEntryPause>();

    public DbSet<InstalledPlugin> InstalledPlugins => Set<InstalledPlugin>();

    public override int SaveChanges()
    {
        ApplyAuditValues();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.Id)
                .HasMaxLength(UserIdMaxLength);

            entity.Property(user => user.UserName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(user => user.DisplayName)
                .HasMaxLength(200);

            entity.Property(user => user.Email)
                .HasMaxLength(320);

            entity.Property(user => user.CreatedAt)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.HasIndex(user => user.UserName)
                .IsUnique();
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            ConfigureAudit(entity);
            ConfigureOwnership(entity);

            entity.Property(customer => customer.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(customer => customer.Notes)
                .HasMaxLength(2000);

            entity.HasIndex(customer => customer.IsActive);
        });

        modelBuilder.Entity<BookingTarget>(entity =>
        {
            ConfigureAudit(entity);
            ConfigureOwnership(entity);

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
            var isPostgres = IsPostgresProvider();
            var statusColumn = isPostgres ? QuotePostgresIdentifier(nameof(WorkEntry.Status)) : nameof(WorkEntry.Status);
            var activeSlotColumn = isPostgres ? QuotePostgresIdentifier("ActiveSlot") : "ActiveSlot";
            var totalPausedSecondsColumn = isPostgres
                ? QuotePostgresIdentifier(nameof(WorkEntry.TotalPausedSeconds))
                : nameof(WorkEntry.TotalPausedSeconds);

            ConfigureAudit(entity);
            ConfigureOwnership(entity);

            entity.ToTable(table => table.HasCheckConstraint(
                "CK_WorkEntries_TotalPausedSeconds_NonNegative",
                $"{totalPausedSecondsColumn} >= 0"));

            entity.Property(entry => entry.ExternalTicketId)
                .HasMaxLength(100);

            entity.Property(entry => entry.Description)
                .HasMaxLength(2000);

            entity.Property(entry => entry.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            var activeSlot = entity.Property<int?>("ActiveSlot");
            var activeSlotSql = $"CASE WHEN {statusColumn} IN ('{WorkEntryStatus.Running}', '{WorkEntryStatus.Paused}') THEN 1 ELSE NULL END";
            if (isPostgres)
            {
                activeSlot.HasComputedColumnSql(activeSlotSql, stored: true);
            }
            else
            {
                activeSlot.HasComputedColumnSql(activeSlotSql);
            }

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
            entity.HasIndex("ActiveSlot")
                .HasDatabaseName("IX_WorkEntries_OneActiveEntry")
                .HasFilter($"{activeSlotColumn} IS NOT NULL")
                .IsUnique();
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
            ConfigureAudit(entity);

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

    private void ApplyAuditValues()
    {
        var now = _clock.UtcNow;
        var userId = NormalizeUserId(_currentUserContext.UserId);

        foreach (var entry in ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Entity.CreatedAt = now;
                }

                entry.Entity.CreatedByUserId = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.CreatedAt).IsModified = false;
                entry.Property(entity => entity.CreatedByUserId).IsModified = false;

                entry.Entity.ModifiedAt = now;
                entry.Entity.ModifiedByUserId = userId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<IUserOwnedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.OwnerUserId = userId;
            }
        }
    }

    private static void ConfigureAudit<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : BaseAuditableEntity
    {
        entity.Property(auditable => auditable.CreatedAt)
            .IsRequired();

        entity.Property(auditable => auditable.CreatedByUserId)
            .HasMaxLength(UserIdMaxLength);

        entity.Property(auditable => auditable.ModifiedByUserId)
            .HasMaxLength(UserIdMaxLength);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(auditable => auditable.CreatedByUserId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(auditable => auditable.ModifiedByUserId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        entity.HasIndex(auditable => auditable.CreatedByUserId);
        entity.HasIndex(auditable => auditable.ModifiedByUserId);
    }

    private static void ConfigureOwnership<TEntity>(EntityTypeBuilder<TEntity> entity)
        where TEntity : class, IUserOwnedEntity
    {
        entity.Property(owned => owned.OwnerUserId)
            .HasMaxLength(UserIdMaxLength);

        entity.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(owned => owned.OwnerUserId)
            .OnDelete(DeleteBehavior.ClientNoAction);

        entity.HasIndex(owned => owned.OwnerUserId);
    }

    private static string? NormalizeUserId(string? userId)
    {
        var trimmed = userId?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private bool IsPostgresProvider()
    {
        return Database.ProviderName == DatabaseOptionsExtensions.PostgresProviderName;
    }

    private static string QuotePostgresIdentifier(string identifier)
    {
        return $"\"{identifier}\"";
    }
}

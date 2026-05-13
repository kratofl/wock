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

    public DbSet<ApplicationUserRole> UserRoles => Set<ApplicationUserRole>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    public DbSet<ActivityCategory> ActivityCategories => Set<ActivityCategory>();

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

        modelBuilder.Entity<ApplicationUserRole>(entity =>
        {
            entity.Property(role => role.UserId)
                .IsRequired()
                .HasMaxLength(UserIdMaxLength);

            entity.Property(role => role.Role)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            entity.HasOne(role => role.User)
                .WithMany(user => user.Roles)
                .HasForeignKey(role => role.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(role => new { role.UserId, role.Role })
                .IsUnique();
            entity.HasIndex(role => role.Role);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            ConfigureAudit(entity);
            ConfigureOwnership(entity);

            entity.Property(customer => customer.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(customer => customer.ContactName)
                .HasMaxLength(200);

            entity.Property(customer => customer.Email)
                .HasMaxLength(320);

            entity.Property(customer => customer.PhoneNumber)
                .HasMaxLength(50);

            entity.Property(customer => customer.BillingAddress)
                .HasMaxLength(2000);

            entity.Property(customer => customer.DefaultHourlyRate)
                .HasPrecision(18, 2);

            entity.Property(customer => customer.Notes)
                .HasMaxLength(2000);

            entity.HasIndex(customer => customer.IsActive);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            ConfigureAudit(entity);
            ConfigureOwnership(entity);

            entity.Property(project => project.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(project => project.Description)
                .HasMaxLength(2000);

            entity.Property(project => project.BudgetHours)
                .HasPrecision(18, 2);

            entity.Property(project => project.BudgetAmount)
                .HasPrecision(18, 2);

            entity.Property(project => project.DefaultHourlyRate)
                .HasPrecision(18, 2);

            entity.Property(project => project.BillingModel)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            entity.Property(project => project.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            entity.HasOne(project => project.Customer)
                .WithMany(customer => customer.Projects)
                .HasForeignKey(project => project.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(project => project.CustomerId);
            entity.HasIndex(project => project.Status);
            entity.HasIndex(project => new { project.CustomerId, project.Name });
        });

        modelBuilder.Entity<ActivityCategory>(entity =>
        {
            entity.Property(category => category.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(category => category.Name)
                .IsUnique();
            entity.HasIndex(category => category.IsActive);
            entity.HasIndex(category => category.SortOrder);

            entity.HasData(
                new ActivityCategory { Id = 1, Name = "Beratung", SortOrder = 10 },
                new ActivityCategory { Id = 2, Name = "Entwicklung", SortOrder = 20 },
                new ActivityCategory { Id = 3, Name = "Design", SortOrder = 30 },
                new ActivityCategory { Id = 4, Name = "Projektmanagement", SortOrder = 40 },
                new ActivityCategory { Id = 5, Name = "Support", SortOrder = 50 },
                new ActivityCategory { Id = 6, Name = "Testing", SortOrder = 60 },
                new ActivityCategory { Id = 7, Name = "Dokumentation", SortOrder = 70 },
                new ActivityCategory { Id = 8, Name = "Administration", SortOrder = 80 },
                new ActivityCategory { Id = 9, Name = "Vertrieb", SortOrder = 90 },
                new ActivityCategory { Id = 10, Name = "Meeting", SortOrder = 100 },
                new ActivityCategory { Id = 11, Name = "Sonstiges", SortOrder = 110 });
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            ConfigureAudit(entity);
            ConfigureOwnership(entity);

            entity.Property(task => task.Title)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(task => task.Description)
                .HasMaxLength(2000);

            entity.Property(task => task.AssignedUserId)
                .HasMaxLength(UserIdMaxLength);

            entity.Property(task => task.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            entity.HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(task => task.ActivityCategory)
                .WithMany(category => category.ProjectTasks)
                .HasForeignKey(task => task.ActivityCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(task => task.AssignedUser)
                .WithMany()
                .HasForeignKey(task => task.AssignedUserId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            entity.HasIndex(task => task.ProjectId);
            entity.HasIndex(task => task.ActivityCategoryId);
            entity.HasIndex(task => task.AssignedUserId);
            entity.HasIndex(task => task.Status);
            entity.HasIndex(task => new { task.ProjectId, task.Title });
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
            var ownerUserIdColumn = isPostgres ? QuotePostgresIdentifier(nameof(WorkEntry.OwnerUserId)) : nameof(WorkEntry.OwnerUserId);
            var activeOwnerSlotColumn = isPostgres ? QuotePostgresIdentifier("ActiveOwnerSlot") : "ActiveOwnerSlot";
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

            entity.Property(entry => entry.BillingCategory)
                .HasMaxLength(100);

            entity.Property(entry => entry.IsBillable)
                .HasDefaultValue(true);

            entity.Property(entry => entry.HourlyRate)
                .HasPrecision(18, 2);

            entity.Property(entry => entry.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(entry => entry.ReviewStatus)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(TimeEntryReviewStatus.Draft);

            entity.Property(entry => entry.ApprovedByUserId)
                .HasMaxLength(UserIdMaxLength);

            entity.Property(entry => entry.RejectionReason)
                .HasMaxLength(2000);

            var activeOwnerSlot = entity.Property<string?>("ActiveOwnerSlot")
                .HasMaxLength(UserIdMaxLength);
            var activeOwnerSlotSql = $"CASE WHEN {statusColumn} IN ('{WorkEntryStatus.Running}', '{WorkEntryStatus.Paused}') THEN COALESCE({ownerUserIdColumn}, '') ELSE NULL END";
            if (isPostgres)
            {
                activeOwnerSlot.HasComputedColumnSql(activeOwnerSlotSql, stored: true);
            }
            else
            {
                activeOwnerSlot.HasComputedColumnSql(activeOwnerSlotSql);
            }

            entity.HasOne(entry => entry.Customer)
                .WithMany(customer => customer.WorkEntries)
                .HasForeignKey(entry => entry.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(entry => entry.BookingTarget)
                .WithMany(target => target.WorkEntries)
                .HasForeignKey(entry => entry.BookingTargetId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(entry => entry.Project)
                .WithMany(project => project.WorkEntries)
                .HasForeignKey(entry => entry.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(entry => entry.ProjectTask)
                .WithMany(task => task.WorkEntries)
                .HasForeignKey(entry => entry.ProjectTaskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(entry => entry.ActivityCategory)
                .WithMany(category => category.WorkEntries)
                .HasForeignKey(entry => entry.ActivityCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(entry => entry.ApprovedByUserId)
                .OnDelete(DeleteBehavior.ClientNoAction);

            entity.HasMany(entry => entry.Pauses)
                .WithOne(pause => pause.WorkEntry)
                .HasForeignKey(pause => pause.WorkEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(entry => entry.Status);
            entity.HasIndex(entry => entry.ReviewStatus);
            entity.HasIndex("ActiveOwnerSlot")
                .HasDatabaseName("IX_WorkEntries_OneActiveEntry")
                .HasFilter($"{activeOwnerSlotColumn} IS NOT NULL")
                .IsUnique();
            entity.HasIndex(entry => entry.StartedAt);
            entity.HasIndex(entry => entry.CustomerId);
            entity.HasIndex(entry => entry.BookingTargetId);
            entity.HasIndex(entry => entry.ProjectId);
            entity.HasIndex(entry => entry.ProjectTaskId);
            entity.HasIndex(entry => entry.ActivityCategoryId);
            entity.HasIndex(entry => entry.ApprovedByUserId);
            entity.HasIndex(entry => entry.ExternalTicketId);
            entity.HasIndex(entry => new { entry.Status, entry.StartedAt });
            entity.HasIndex(entry => new { entry.OwnerUserId, entry.ReviewStatus });
            entity.HasIndex(entry => new { entry.CustomerId, entry.StartedAt });
            entity.HasIndex(entry => new { entry.ProjectId, entry.StartedAt });
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

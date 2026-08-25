using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Api.Models;

namespace AtcSimulationFramework.Api.Data;

public class AtcDbContext : DbContext
{
    public AtcDbContext(DbContextOptions<AtcDbContext> options) : base(options)
    {
    }

    public virtual DbSet<SimulationRun> SimulationRuns { get; set; } = null!;
    public virtual DbSet<Aircraft> Aircraft { get; set; } = null!;
    public virtual DbSet<PositionLog> PositionLogs { get; set; } = null!;
    public virtual DbSet<ConflictEvent> ConflictEvents { get; set; } = null!;
    public virtual DbSet<VectorCommand> VectorCommands { get; set; } = null!;
    public virtual DbSet<AppUser> AppUsers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SimulationRun Configuration
        modelBuilder.Entity<SimulationRun>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedSimulations)
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // Aircraft Configuration
        modelBuilder.Entity<Aircraft>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Callsign).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => new { e.SimulationRunId, e.Callsign });
            entity.HasIndex(e => new { e.SimulationRunId, e.Status });

            entity.HasOne(e => e.SimulationRun)
                  .WithMany(s => s.AircraftList)
                  .HasForeignKey(e => e.SimulationRunId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // PositionLog Configuration
        modelBuilder.Entity<PositionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SimulationRunId, e.TickNumber });
            entity.HasIndex(e => new { e.AircraftId, e.TickNumber });

            entity.HasOne(e => e.SimulationRun)
                  .WithMany(s => s.PositionLogs)
                  .HasForeignKey(e => e.SimulationRunId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Aircraft)
                  .WithMany(a => a.PositionLogs)
                  .HasForeignKey(e => e.AircraftId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ConflictEvent Configuration
        modelBuilder.Entity<ConflictEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SimulationRunId, e.IsResolved });

            entity.HasOne(e => e.SimulationRun)
                  .WithMany(s => s.ConflictEvents)
                  .HasForeignKey(e => e.SimulationRunId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PrimaryAircraft)
                  .WithMany()
                  .HasForeignKey(e => e.PrimaryAircraftId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.SecondaryAircraft)
                  .WithMany()
                  .HasForeignKey(e => e.SecondaryAircraftId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // VectorCommand Configuration
        modelBuilder.Entity<VectorCommand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SimulationRunId, e.Status });

            entity.HasOne(e => e.SimulationRun)
                  .WithMany(s => s.VectorCommands)
                  .HasForeignKey(e => e.SimulationRunId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Aircraft)
                  .WithMany(a => a.VectorCommands)
                  .HasForeignKey(e => e.AircraftId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.IssuedByUser)
                  .WithMany(u => u.IssuedCommands)
                  .HasForeignKey(e => e.IssuedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // AppUser Configuration
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}

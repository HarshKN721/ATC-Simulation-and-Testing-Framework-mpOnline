using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Api.Entities;

namespace AtcSimulationFramework.Api.Data;

public class AtcDbContext : DbContext
{
    public AtcDbContext(DbContextOptions<AtcDbContext> options) : base(options)
    {
    }

    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<PositionLog> PositionLogs => Set<PositionLog>();
    public DbSet<ConflictEvent> ConflictEvents => Set<ConflictEvent>();
    public DbSet<VectorCommand> VectorCommands => Set<VectorCommand>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SimulationRun relationships
        modelBuilder.Entity<SimulationRun>()
            .HasOne(s => s.AppUser)
            .WithMany(u => u.SimulationRuns)
            .HasForeignKey(s => s.AppUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Aircraft relationships
        modelBuilder.Entity<Aircraft>()
            .HasOne(a => a.SimulationRun)
            .WithMany(s => s.Aircraft)
            .HasForeignKey(a => a.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // PositionLog relationships
        modelBuilder.Entity<PositionLog>()
            .HasOne(p => p.SimulationRun)
            .WithMany(s => s.PositionLogs)
            .HasForeignKey(p => p.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PositionLog>()
            .HasOne(p => p.Aircraft)
            .WithMany(a => a.PositionLogs)
            .HasForeignKey(p => p.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);

        // ConflictEvent relationships
        modelBuilder.Entity<ConflictEvent>()
            .HasOne(c => c.SimulationRun)
            .WithMany(s => s.ConflictEvents)
            .HasForeignKey(c => c.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConflictEvent>()
            .HasOne(c => c.AircraftA)
            .WithMany()
            .HasForeignKey(c => c.AircraftAId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConflictEvent>()
            .HasOne(c => c.AircraftB)
            .WithMany()
            .HasForeignKey(c => c.AircraftBId)
            .OnDelete(DeleteBehavior.Restrict);

        // VectorCommand relationships
        modelBuilder.Entity<VectorCommand>()
            .HasOne(v => v.SimulationRun)
            .WithMany(s => s.VectorCommands)
            .HasForeignKey(v => v.SimulationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VectorCommand>()
            .HasOne(v => v.ConflictEvent)
            .WithMany(c => c.VectorCommands)
            .HasForeignKey(v => v.ConflictEventId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<VectorCommand>()
            .HasOne(v => v.Aircraft)
            .WithMany(a => a.VectorCommands)
            .HasForeignKey(v => v.AircraftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

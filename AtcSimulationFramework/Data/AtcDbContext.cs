namespace AtcSimulationFramework.Data;

using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Models;

public class AtcDbContext : DbContext
{
    public AtcDbContext(DbContextOptions<AtcDbContext> options) : base(options) { }

    public DbSet<Aircraft> Aircraft => Set<Aircraft>();
    public DbSet<ConflictEvent> ConflictEvents => Set<ConflictEvent>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<PositionLog> PositionLogs => Set<PositionLog>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<VectorCommand> VectorCommands => Set<VectorCommand>();

    /// <summary>
    /// Loads aircraft for a run and copies the latest PositionLog into
    /// the in-memory kinematics fields. Shared by the tick loop and API.
    /// </summary>
    public async Task<List<Aircraft>> GetAircraftWithLatestPositionsAsync(
        int runId, CancellationToken ct = default)
    {
        var aircraft = await Aircraft
            .Where(a => a.RunId == runId)
            .ToListAsync(ct);

        if (aircraft.Count == 0)
            return aircraft;

        var aircraftIds = aircraft.Select(a => a.AircraftId).ToList();

        var latestPositions = await PositionLogs
            .AsNoTracking()
            .Where(p => aircraftIds.Contains(p.AircraftId))
            .GroupBy(p => p.AircraftId)
            .Select(g => g.OrderByDescending(p => p.Timestamp).First())
            .ToDictionaryAsync(p => p.AircraftId, ct);

        foreach (var ac in aircraft)
        {
            if (!latestPositions.TryGetValue(ac.AircraftId, out var pos))
                continue;

            ac.Latitude   = (double)pos.Latitude;
            ac.Longitude  = (double)pos.Longitude;
            ac.AltitudeFt = pos.AltitudeFt;
            ac.HeadingDeg = (double)pos.HeadingDeg;
            ac.SpeedKts   = pos.SpeedKts;
        }

        return aircraft;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Aircraft>().ToTable("Aircraft");
        modelBuilder.Entity<ConflictEvent>().ToTable("ConflictEvent");
        modelBuilder.Entity<SimulationRun>().ToTable("SimulationRun");
        modelBuilder.Entity<PositionLog>().ToTable("PositionLog");
        modelBuilder.Entity<AppUser>().ToTable("AppUser");
        modelBuilder.Entity<VectorCommand>().ToTable("VectorCommand");

        modelBuilder.Entity<Aircraft>(entity =>
        {
            entity.Property(a => a.Callsign).HasMaxLength(10).IsRequired();
            entity.Property(a => a.Icao24).HasMaxLength(6).IsRequired();
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(50).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<SimulationRun>(entity =>
        {
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<PositionLog>(entity =>
        {
            entity.Property(p => p.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(p => p.Longitude).HasColumnType("decimal(9,6)");
            entity.Property(p => p.HeadingDeg).HasColumnType("decimal(5,2)");
        });

        modelBuilder.Entity<ConflictEvent>(entity =>
        {
            entity.Property(c => c.HorizontalDistNm).HasColumnType("decimal(6,3)");
            entity.Property(c => c.ResolutionAction).HasMaxLength(50);

            entity.HasOne(c => c.AircraftA)
                .WithMany()
                .HasForeignKey(c => c.AircraftAId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(c => c.AircraftB)
                .WithMany()
                .HasForeignKey(c => c.AircraftBId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<VectorCommand>(entity =>
        {
            entity.Property(v => v.CommandType).HasMaxLength(20).IsRequired();
            entity.Property(v => v.Value).HasColumnType("decimal(6,2)");
        });
    }
}

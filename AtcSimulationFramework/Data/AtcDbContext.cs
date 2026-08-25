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
}

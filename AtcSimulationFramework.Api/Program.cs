using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Api.Background;
using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Interfaces;
using AtcSimulationFramework.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EF Core Database-First Context (using InMemory provider for runtime execution)
builder.Services.AddDbContext<AtcDbContext>(options =>
    options.UseInMemoryDatabase("AtcSimulationDb"));

// Register Application Services and Agent with appropriate DI Lifetime
builder.Services.AddScoped<IControllerAgent, AutomatedControllerAgent>();
builder.Services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
builder.Services.AddScoped<ISimulationService, SimulationService>();

// Register BackgroundService Simulation Loop
builder.Services.AddHostedService<SimulationBackgroundService>();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Partial Program class declaration for WebApplicationFactory testing support
public partial class Program { }

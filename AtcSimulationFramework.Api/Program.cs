using AtcSimulationFramework.Api.Data;
using AtcSimulationFramework.Api.Services.ConflictDetection;
using AtcSimulationFramework.Api.Services.Kinematics;
using AtcSimulationFramework.Api.Services.Simulation;
using AtcSimulationFramework.Api.Services.VectorCommands;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers and JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Database Context Configuration
var connectionString = builder.Configuration.GetConnectionString("AtcDbConnection") ?? "Data Source=atc_simulation.db";
builder.Services.AddDbContext<AtcDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Register Domain & Simulation Services
builder.Services.AddSingleton<IKinematicsEngine, KinematicsEngine>();
builder.Services.AddScoped<IConflictDetectionService, ConflictDetectionService>();
builder.Services.AddScoped<IVectorCommandService, VectorCommandService>();
builder.Services.AddScoped<ISimulationEngine, SimulationEngine>();

// Register BackgroundService Tick Loop
builder.Services.AddHostedService<AtcSimulationBackgroundService>();

// CORS Setup for frontend polling
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed & ensure DB created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AtcDbContext>();
    await DbInitializer.InitializeAsync(dbContext);
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ATC Simulation API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();

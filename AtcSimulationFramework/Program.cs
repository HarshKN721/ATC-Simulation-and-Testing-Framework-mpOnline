using Microsoft.EntityFrameworkCore;
using AtcSimulationFramework.Data;
using AtcSimulationFramework.Services;

var builder = WebApplication.CreateBuilder(args);
// Yeh waala program mai check kar lunga (HKN)
// ---------------------------------------------------------------------
// 1. Controllers — REST API endpoints 
// ---------------------------------------------------------------------
builder.Services.AddControllers();

// ---------------------------------------------------------------------
// 2. OpenAPI / Swagger — lets us test endpoints live at /swagger
// ---------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------------------------------------------------
// 3. EF Core DbContext — Database-First, scaffolded from ATC_DB
//    (Shriyam's schema). Connection string lives in appsettings.json.
//    NOTE: once Scaffold-DbContext is run, the generated class name
//    might differ slightly (e.g. AtcDbContext) — update this line
//    to match whatever name the scaffolding tool actually generates.
// ---------------------------------------------------------------------
builder.Services.AddDbContext<AtcDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AtcDbConnection")));

// ---------------------------------------------------------------------
// 4. ConflictDetector — (Shayad Aditya ka hai yeh waala) Haversine-based conflict logic.
//    Registered as Scoped since it may eventually need DbContext access.
// ---------------------------------------------------------------------
builder.Services.AddScoped<ConflictDetector>();

// ---------------------------------------------------------------------
// 5. Automated Controller Agent — rule-based resolution logic,
//    registered against its interface so the implementation is
//    swappable later without touching anything that depends on it. (isko bhi pickup karlo yaar)
// ---------------------------------------------------------------------
builder.Services.AddScoped<IControllerAgent, AutomatedControllerAgent>();

// ---------------------------------------------------------------------
// 6. Simulation Tick Loop — BackgroundService, runs
//    continuously in the background independent of HTTP requests.
//    Registered as a HostedService so ASP.NET Core starts it
//    automatically when the app starts. (yeh waala bhi pick up karna baaki hai)
// ---------------------------------------------------------------------
builder.Services.AddHostedService<SimulationTickService>();

var app = builder.Build();

// ---------------------------------------------------------------------
// Middleware pipeline
// ---------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // serves wwwroot/index.html + app.js (Aryaman's frontend)
app.UseAuthorization();
app.MapControllers();

app.Run();
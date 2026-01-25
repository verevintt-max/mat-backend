using Microsoft.EntityFrameworkCore;
using WorkshopApi.Data;
using WorkshopApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Workshop API", 
        Version = "v1",
        Description = "API для системы учета мастерской - управление материалами, производством и продажами"
    });
});

// Database - support DATABASE_URL from Render.com
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Convert postgres:// URL to Npgsql connection string if needed
if (!string.IsNullOrEmpty(connectionString) && connectionString.StartsWith("postgres://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

// Use PostgreSQL
builder.Services.AddDbContext<WorkshopDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<OperationHistoryService>();
builder.Services.AddScoped<MaterialService>();
builder.Services.AddScoped<MaterialReceiptService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<FinishedProductService>();
builder.Services.AddScoped<ReportService>();

// CORS - allow all origins for now (can be restricted later)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
Console.WriteLine("CORS: Allowing all origins");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workshop API v1");
    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection(); // Отключено для разработки

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Test database connection on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WorkshopDbContext>();
    try
    {
        // Just test the connection, don't try to migrate
        await db.Database.CanConnectAsync();
        Console.WriteLine("Database connection successful.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection error: {ex.Message}");
    }
}

app.Run();


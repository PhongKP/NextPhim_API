using Microsoft.EntityFrameworkCore;
using NextPhimAPI.Application.Interfaces;
using NextPhimAPI.Infrastructure.Persistence;
using NextPhimAPI.Infrastructure.Redis;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure for deployment on platforms
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Add services to the container.
builder.Services.AddControllers();


// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextPhimFE", policy =>
    {
        policy.WithOrigins(
                "https://www.nextphim.app",
                "http://localhost:4200"
              )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure PostgreSQL with EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresDb")));

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString!);
    return ConnectionMultiplexer.Connect(configuration);
});

// Register MovieStatService
builder.Services.AddScoped<IMovieStatsService, MovieStatService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseCors("AllowNextPhimFE");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NextPhim API");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using backend.Business.Services;
using backend.Domain.Interfaces;
using backend.Infrastructure.Persistence;
using backend.Infrastructure.Spotify;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<SpotifyOptions>(builder.Configuration.GetSection(SpotifyOptions.Section));
builder.Services.AddScoped<ISpotifyService, SpotifyService>();

builder.Services.AddScoped<IPlaylistRepository, PlaylistRepository>();
builder.Services.AddScoped<ICanvasRepository, CanvasRepository>();
builder.Services.AddScoped<PlaylistAnalyzerService>();
builder.Services.AddScoped<CanvasService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(opt => opt.AddDefaultPolicy(policy =>
policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

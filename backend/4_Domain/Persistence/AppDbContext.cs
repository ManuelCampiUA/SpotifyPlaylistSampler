using backend.Business.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlaylistCache> Playlists => Set<PlaylistCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlaylistCache>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SpotifyId);
            entity.Property(e => e.SpotifyId).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.ResultJson).IsRequired();
        });
    }
}

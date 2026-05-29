using backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<PlaylistCache> Playlists => Set<PlaylistCache>();
    public DbSet<CanvasNode> CanvasNodes => Set<CanvasNode>();
    public DbSet<CanvasEdge> CanvasEdges => Set<CanvasEdge>();

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

        modelBuilder.Entity<CanvasNode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ReferenceId).IsUnique();
            entity.Property(e => e.NodeType).IsRequired();
            entity.Property(e => e.ReferenceId).IsRequired();
            entity.Property(e => e.Label).IsRequired();
        });

        modelBuilder.Entity<CanvasEdge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EdgeType).IsRequired();

            entity.HasOne(e => e.SourceNode)
                .WithMany()
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetNode)
                .WithMany()
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

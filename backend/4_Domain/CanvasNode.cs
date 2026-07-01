namespace backend.Domain;

public class CanvasNode
{
    public int Id { get; set; }
    public string NodeType { get; set; } = default!;

    public string ReferenceId { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string? Artist { get; set; }
    public string? ImageUrl { get; set; }
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public string? Color { get; set; }
    public string? ParentPlaylistId { get; set; }
    public string? ParentPlaylistName { get; set; }
}

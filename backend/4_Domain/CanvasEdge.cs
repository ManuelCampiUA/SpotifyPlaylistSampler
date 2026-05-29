namespace backend.Domain;

public class CanvasEdge
{
    public int Id { get; set; }

    public int SourceNodeId { get; set; }
    public CanvasNode SourceNode { get; set; } = default!;

    public int TargetNodeId { get; set; }
    public CanvasNode TargetNode { get; set; } = default!;

    /// <summary>"intra-playlist" (auto-generated) or "custom" (user-created).</summary>
    public string EdgeType { get; set; } = default!;
}

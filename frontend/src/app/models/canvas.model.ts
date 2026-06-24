export interface CanvasNodeModel {
  id: number;
  nodeType: string;
  referenceId: string;
  label: string;
  artist?: string;
  imageUrl?: string;
  positionX: number;
  positionY: number;
  color?: string;
  parentPlaylistId?: string;
  parentPlaylistName?: string;
}

export interface CanvasEdgeModel {
  id: number;
  sourceNodeId: number;
  targetNodeId: number;
  edgeType: string;
}

export interface CanvasState {
  nodes: CanvasNodeModel[];
  edges: CanvasEdgeModel[];
}

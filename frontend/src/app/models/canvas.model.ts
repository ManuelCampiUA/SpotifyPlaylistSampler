export interface CanvasNodeModel {
  id: number;
  nodeType: 'playlist' | 'track';
  referenceId: string;
  label: string;
  positionX: number;
  positionY: number;
  color?: string;
  parentPlaylistId?: string;
}

export interface CanvasEdgeModel {
  id: number;
  sourceNodeId: number;
  targetNodeId: number;
  edgeType: 'intra-playlist' | 'custom';
}

export interface CanvasState {
  nodes: CanvasNodeModel[];
  edges: CanvasEdgeModel[];
}

export interface RenderedEdge extends CanvasEdgeModel {
  x1: number;
  y1: number;
  x2: number;
  y2: number;
}

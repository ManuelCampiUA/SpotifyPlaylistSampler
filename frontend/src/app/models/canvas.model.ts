export interface CanvasNodeModel {
  id: number;
  nodeType: string;
  referenceId: string;
  label: string;
  artist?: string;
  positionX: number;
  positionY: number;
  color?: string;
  parentPlaylistId?: string;
}

export interface CanvasState {
  nodes: CanvasNodeModel[];
}

import { Injectable, inject, signal, resource } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { CanvasState, CanvasNodeModel, CanvasEdgeModel } from '../models/canvas.model';

@Injectable({ providedIn: 'root' })
export class CanvasService {
  readonly #http = inject(HttpClient);
  readonly #refreshTrigger = signal(0);

  readonly canvasResource = resource<CanvasState | undefined, number>({
    params: this.#refreshTrigger,
    loader: () => firstValueFrom(this.#http.get<CanvasState>('/api/canvas')),
  });

  refresh(): void {
    this.#refreshTrigger.update(v => v + 1);
  }

  addPlaylistNode(spotifyId: string): Observable<CanvasNodeModel> {
    return this.#http.post<CanvasNodeModel>('/api/canvas/playlist', { spotifyId });
  }

  addTrackNode(spotifyId: string, trackIndex: number): Observable<CanvasNodeModel> {
    return this.#http.post<CanvasNodeModel>('/api/canvas/track', { spotifyId, trackIndex });
  }

  removeNode(nodeId: number): Observable<void> {
    return this.#http.delete<void>(`/api/canvas/nodes/${nodeId}`);
  }

  updateNodePosition(id: number, positionX: number, positionY: number): Observable<CanvasNodeModel> {
    return this.#http.put<CanvasNodeModel>(`/api/canvas/nodes/${id}`, { positionX, positionY });
  }

  batchUpdatePositions(items: { id: number; positionX: number; positionY: number }[]): Observable<void> {
    return this.#http.put<void>('/api/canvas/nodes/batch', items);
  }

  createEdge(sourceNodeId: number, targetNodeId: number): Observable<CanvasEdgeModel> {
    return this.#http.post<CanvasEdgeModel>('/api/canvas/edges', { sourceNodeId, targetNodeId });
  }

  removeEdge(edgeId: number): Observable<void> {
    return this.#http.delete<void>(`/api/canvas/edges/${edgeId}`);
  }

  clearAll(): Observable<void> {
    return this.#http.delete<void>('/api/canvas/clear');
  }
}

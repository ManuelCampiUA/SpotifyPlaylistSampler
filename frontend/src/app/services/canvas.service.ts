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

  addPlaylist(spotifyId: string): Observable<CanvasState> {
    return this.#http.post<CanvasState>('/api/canvas/playlist', { spotifyId });
  }

  removePlaylist(spotifyId: string): Observable<CanvasState> {
    return this.#http.delete<CanvasState>(`/api/canvas/playlist/${spotifyId}`);
  }

  updateNodePosition(id: number, positionX: number, positionY: number): Observable<CanvasNodeModel> {
    return this.#http.put<CanvasNodeModel>(`/api/canvas/nodes/${id}`, { positionX, positionY });
  }

  createEdge(sourceNodeId: number, targetNodeId: number): Observable<CanvasEdgeModel> {
    return this.#http.post<CanvasEdgeModel>('/api/canvas/edges', { sourceNodeId, targetNodeId });
  }

  removeEdge(id: number): Observable<void> {
    return this.#http.delete<void>(`/api/canvas/edges/${id}`);
  }
}

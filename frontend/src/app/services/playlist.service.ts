import { Injectable, inject, signal, resource, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom, Observable } from 'rxjs';
import { PlaylistResult, PlaylistSummary } from '../models/playlist.model';

type PlaylistRequest =
  | { type: 'analyze'; url: string }
  | { type: 'select'; id: string };

@Injectable({ providedIn: 'root' })
export class PlaylistService {
  readonly #http = inject(HttpClient);
  readonly #activeRequest = signal<PlaylistRequest | undefined>(undefined);

  readonly playlistResource = resource<PlaylistResult | undefined, PlaylistRequest | undefined>({
    params: this.#activeRequest,
    loader: ({ params }) => {
      if (!params) return Promise.resolve(undefined);
      if (params.type === 'analyze') {
        return firstValueFrom(
          this.#http.post<PlaylistResult>('/api/playlist/analyze', { url: params.url }),
        );
      }
      return firstValueFrom(
        this.#http.get<PlaylistResult>(`/api/playlist/${params.id}`),
      );
    },
  });

  analyze(url: string): void {
    const current = this.#activeRequest();
    if (current?.type === 'analyze' && current.url === url) {
      this.#activeRequest.set(undefined);
      Promise.resolve().then(() => this.#activeRequest.set({ type: 'analyze', url }));
    } else {
      this.#activeRequest.set({ type: 'analyze', url });
    }
  }

  selectById(id: string): void {
    this.#activeRequest.set({ type: 'select', id });
  }

  readonly historyResource = resource<PlaylistSummary[], boolean>({
    params: signal<boolean>(true),
    loader: () =>
      firstValueFrom(this.#http.get<PlaylistSummary[]>('/api/playlist/history')),
  });

  constructor() {
    effect(() => {
      const val = this.playlistResource.value();
      const loading = this.playlistResource.isLoading();
      const req = this.#activeRequest();
      if (val !== undefined && !loading && req?.type === 'analyze') {
        this.historyResource.reload();
      }
    });
  }

  getPlaylistResult(spotifyId: string): Observable<PlaylistResult> {
    return this.#http.get<PlaylistResult>(`/api/playlist/${spotifyId}`);
  }
}

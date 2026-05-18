import { Injectable, inject, signal, resource } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { PlaylistResult } from '../models/playlist.model';

const API_URL = '/api/playlist/analyze';

@Injectable({ providedIn: 'root' })
export class PlaylistService {
  readonly #http = inject(HttpClient);
  readonly #requestUrl = signal<string | undefined>(undefined);

  readonly playlistResource = resource<PlaylistResult, string | undefined>({
    params: this.#requestUrl,
    loader: ({ params: url }) => firstValueFrom(this.#http.post<PlaylistResult>(API_URL, { url })),
  });

  analyze(url: string): void {
    // Force reload even if same URL by resetting first
    if (this.#requestUrl() === url) {
      this.#requestUrl.set(undefined);
      Promise.resolve().then(() => this.#requestUrl.set(url));
    } else {
      this.#requestUrl.set(url);
    }
  }
}

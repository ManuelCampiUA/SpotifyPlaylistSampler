import { Component, Signal, computed, inject, signal } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { PlaylistService } from '../../services/playlist.service';
import { PlaylistResult, PlaylistSummary } from '../../models/playlist.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    JsonPipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    MatProgressBarModule,
    MatTooltipModule,
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly playlistService = inject(PlaylistService);

  readonly urlInput = signal('');

  readonly isValidUrl = computed(() =>
    /open\.spotify\.com\/playlist\/[A-Za-z0-9]+/.test(this.urlInput()),
  );

  readonly resource = this.playlistService.playlistResource;
  readonly isLoading = this.playlistService.playlistResource.isLoading;
  readonly result: Signal<PlaylistResult | undefined> = this.playlistService.playlistResource.value;
  readonly hasError = computed(() => !!this.playlistService.playlistResource.error());
  readonly errorMessage = computed(() => {
    const err = this.playlistService.playlistResource.error();
    if (!err) return null;
    if (err instanceof Error) return err.message;
    return "Si è verificato un errore durante l'analisi della playlist.";
  });

  readonly hasResult = computed(() => !!this.result());

  // Library sidebar
  readonly history: Signal<PlaylistSummary[] | undefined> =
    this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;
  readonly hasHistory = computed(() => (this.history()?.length ?? 0) > 0);
  readonly selectedPlaylistId = signal<string | undefined>(undefined);

  analyze(): void {
    if (this.isValidUrl()) {
      this.selectedPlaylistId.set(undefined);
      this.playlistService.analyze(this.urlInput().trim());
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.analyze();
    }
  }

  selectPlaylist(spotifyId: string): void {
    this.selectedPlaylistId.set(spotifyId);
    this.playlistService.selectById(spotifyId);
  }

  refreshHistory(): void {
    this.playlistService.historyResource.reload();
  }

  deletePlaylist(event: Event, spotifyId: string): void {
    event.stopPropagation();
    this.playlistService.deletePlaylist(spotifyId).subscribe({
      next: () => {
        if (this.selectedPlaylistId() === spotifyId) {
          this.selectedPlaylistId.set(undefined);
        }
        this.playlistService.historyResource.reload();
      },
    });
  }

  formatDuration(ms: number): string {
    const min = Math.floor(ms / 60000);
    const sec = Math.floor((ms % 60000) / 1000);
    return `${min}:${sec.toString().padStart(2, '0')}`;
  }
}

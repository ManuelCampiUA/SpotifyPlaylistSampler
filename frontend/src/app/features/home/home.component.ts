import { Component, Signal, computed, inject, signal } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { PlaylistResult } from '../../models/playlist.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    JsonPipe,
    FormsModule,
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

  analyze(): void {
    if (this.isValidUrl()) {
      this.playlistService.analyze(this.urlInput().trim());
    }
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      this.analyze();
    }
  }

  formatDuration(ms: number): string {
    const min = Math.floor(ms / 60000);
    const sec = Math.floor((ms % 60000) / 1000);
    return `${min}:${sec.toString().padStart(2, '0')}`;
  }
}

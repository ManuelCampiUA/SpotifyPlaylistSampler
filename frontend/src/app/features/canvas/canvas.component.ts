import {
  Component,
  inject,
  signal,
  computed,
  effect,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DragDropModule, CdkDragEnd } from '@angular/cdk/drag-drop';

import { CanvasService } from '../../services/canvas.service';
import { PlaylistService } from '../../services/playlist.service';
import { CanvasNodeModel, CanvasState } from '../../models/canvas.model';
import { PlaylistSummary } from '../../models/playlist.model';

@Component({
  selector: 'app-canvas',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatDividerModule,
    DragDropModule,
  ],
  templateUrl: './canvas.component.html',
  styleUrl: './canvas.component.scss',
})
export class CanvasComponent {
  private readonly canvasService = inject(CanvasService);
  private readonly playlistService = inject(PlaylistService);
  private readonly snackBar = inject(MatSnackBar);

  // ── Block state
  readonly #blocks = signal<CanvasNodeModel[]>([]);

  readonly blocks = this.#blocks.asReadonly();

  readonly onCanvasPlaylistIds = computed(
    () => new Set(
      this.#blocks()
        .map(b => b.parentPlaylistId)
        .filter((id): id is string => !!id)
    )
  );

  readonly isLoading = this.canvasService.canvasResource.isLoading;

  // ── Library sidebar
  readonly history = this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;

  constructor() {
    effect(() => {
      const val = this.canvasService.canvasResource.value();
      if (val) this.#blocks.set(val.nodes);
    });
  }

  // ── Drag handling

  onDragEnded(event: CdkDragEnd, block: CanvasNodeModel): void {
    const newX = block.positionX + event.distance.x;
    const newY = block.positionY + event.distance.y;

    // Reset CDK transform; absolute position takes over
    event.source.reset();

    // Update local signal
    this.#blocks.update(blocks =>
      blocks.map(b => b.id === block.id ? { ...b, positionX: newX, positionY: newY } : b)
    );

    // Persist to backend
    this.canvasService.updateNodePosition(block.id, newX, newY).subscribe();
  }

  // ── Sidebar helpers

  isOnCanvas(spotifyId: string): boolean {
    return this.onCanvasPlaylistIds().has(spotifyId);
  }

  displayName(p: PlaylistSummary): string {
    return p.playlistName || (p as any).name || p.spotifyId;
  }

  getPlaylistColor(spotifyId: string): string {
    const block = this.#blocks().find(b => b.parentPlaylistId === spotifyId);
    return block?.color ?? '#1ed760';
  }

  addToCanvas(spotifyId: string): void {
    this.canvasService.addPlaylistNode(spotifyId).subscribe({
      next: (state: CanvasState) => this.#blocks.set(state.nodes),
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore durante l\'aggiunta', 'Chiudi', { duration: 3000 }),
    });
  }

  removeFromCanvas(spotifyId: string): void {
    this.canvasService.removePlaylistNode(spotifyId).subscribe({
      next: () => this.#blocks.update(blocks => blocks.filter(b => b.parentPlaylistId !== spotifyId)),
      error: () =>
        this.snackBar.open('Errore durante la rimozione', 'Chiudi', { duration: 3000 }),
    });
  }
}

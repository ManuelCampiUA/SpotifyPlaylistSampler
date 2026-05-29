import {
  Component,
  inject,
  signal,
  computed,
  effect,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';

import { CanvasService } from '../../services/canvas.service';
import { PlaylistService } from '../../services/playlist.service';
import { CanvasNodeModel, CanvasEdgeModel, RenderedEdge } from '../../models/canvas.model';
import { PlaylistSummary } from '../../models/playlist.model';

const DRAG_THRESHOLD_PX = 5;

interface DragState {
  nodeId: number;
  startMouseX: number;
  startMouseY: number;
  startNodeX: number;
  startNodeY: number;
}

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
  ],
  templateUrl: './canvas.component.html',
  styleUrl: './canvas.component.scss',
})
export class CanvasComponent {
  @ViewChild('svgEl') svgEl!: ElementRef<SVGElement>;

  private readonly canvasService = inject(CanvasService);
  private readonly playlistService = inject(PlaylistService);
  private readonly snackBar = inject(MatSnackBar);

  readonly SVG_WIDTH = 4000;
  readonly SVG_HEIGHT = 3000;

  // ── Local canvas state (immediately reactive, saved async) ───────────────
  readonly #localNodes = signal<CanvasNodeModel[]>([]);
  readonly #localEdges = signal<CanvasEdgeModel[]>([]);

  readonly nodes = this.#localNodes.asReadonly();
  readonly edges = this.#localEdges.asReadonly();

  readonly nodeMap = computed(() => {
    const map = new Map<number, CanvasNodeModel>();
    for (const n of this.#localNodes()) map.set(n.id, n);
    return map;
  });

  readonly renderedEdges = computed<RenderedEdge[]>(() => {
    const map = this.nodeMap();
    return this.#localEdges().map(edge => ({
      ...edge,
      x1: map.get(edge.sourceNodeId)?.positionX ?? 0,
      y1: map.get(edge.sourceNodeId)?.positionY ?? 0,
      x2: map.get(edge.targetNodeId)?.positionX ?? 0,
      y2: map.get(edge.targetNodeId)?.positionY ?? 0,
    }));
  });

  readonly onCanvasPlaylistIds = computed(
    () => new Set(this.#localNodes().filter(n => n.nodeType === 'playlist').map(n => n.referenceId))
  );

  readonly isLoading = this.canvasService.canvasResource.isLoading;

  // ── Interaction state ────────────────────────────────────────────────────
  readonly connectMode = signal(false);
  readonly pendingSource = signal<CanvasNodeModel | null>(null);
  readonly hasCustomEdges = computed(() => this.#localEdges().some(e => e.edgeType === 'custom'));

  // Regular properties for drag (not signals — never used in template)
  private dragState: DragState | null = null;
  private hasDragged = false;

  // ── Library sidebar ──────────────────────────────────────────────────────
  readonly history = this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;

  constructor() {
    // Sync resource into local signals on first load
    effect(() => {
      const val = this.canvasService.canvasResource.value();
      if (val) {
        this.#localNodes.set(val.nodes);
        this.#localEdges.set(val.edges);
      }
    });
  }

  // ── Sidebar helpers ──────────────────────────────────────────────────────

  isOnCanvas(spotifyId: string): boolean {
    return this.onCanvasPlaylistIds().has(spotifyId);
  }

  displayName(p: PlaylistSummary): string {
    // Backend sends `name`, frontend model has `playlistName` — handle both
    return p.playlistName || (p as any).name || p.spotifyId;
  }

  getPlaylistColor(spotifyId: string): string {
    const node = this.#localNodes().find(n => n.nodeType === 'playlist' && n.referenceId === spotifyId);
    return node?.color ?? '#1ed760';
  }

  addToCanvas(spotifyId: string): void {
    this.canvasService.addPlaylist(spotifyId).subscribe({
      next: (state) => {
        this.#localNodes.set(state.nodes);
        this.#localEdges.set(state.edges);
      },
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore durante l\'aggiunta', 'Chiudi', { duration: 3000 }),
    });
  }

  removeFromCanvas(spotifyId: string): void {
    this.canvasService.removePlaylist(spotifyId).subscribe({
      next: (state) => {
        this.#localNodes.set(state.nodes);
        this.#localEdges.set(state.edges);
      },
      error: () =>
        this.snackBar.open('Errore durante la rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  // ── Connect mode ─────────────────────────────────────────────────────────

  toggleConnectMode(): void {
    this.connectMode.update(v => !v);
    this.pendingSource.set(null);
  }

  // ── Node events ──────────────────────────────────────────────────────────

  onNodeMouseDown(event: MouseEvent, node: CanvasNodeModel): void {
    event.stopPropagation();
    event.preventDefault();
    this.hasDragged = false;

    if (this.connectMode()) return; // drag disabled in connect mode

    this.dragState = {
      nodeId: node.id,
      startMouseX: event.clientX,
      startMouseY: event.clientY,
      startNodeX: node.positionX,
      startNodeY: node.positionY,
    };
  }

  onNodeClick(event: MouseEvent, node: CanvasNodeModel): void {
    event.stopPropagation();

    if (this.hasDragged) return; // was a drag, not a click
    if (!this.connectMode()) return;

    const pending = this.pendingSource();

    if (!pending) {
      this.pendingSource.set(node);
      return;
    }

    if (pending.id === node.id) {
      this.pendingSource.set(null); // deselect
      return;
    }

    this.pendingSource.set(null);
    this.canvasService.createEdge(pending.id, node.id).subscribe({
      next: (edge) => this.#localEdges.update(edges => [...edges, edge]),
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore durante il collegamento', 'Chiudi', { duration: 3000 }),
    });
  }

  // ── SVG mouse events ─────────────────────────────────────────────────────

  onSvgMouseMove(event: MouseEvent): void {
    if (!this.dragState) return;

    const dx = event.clientX - this.dragState.startMouseX;
    const dy = event.clientY - this.dragState.startMouseY;

    if (!this.hasDragged && Math.hypot(dx, dy) < DRAG_THRESHOLD_PX) return;
    this.hasDragged = true;

    const newX = this.dragState.startNodeX + dx;
    const newY = this.dragState.startNodeY + dy;
    const nodeId = this.dragState.nodeId;

    this.#localNodes.update(nodes =>
      nodes.map(n => n.id === nodeId ? { ...n, positionX: newX, positionY: newY } : n)
    );
  }

  onSvgMouseUp(): void {
    if (!this.dragState) return;

    if (this.hasDragged) {
      const node = this.nodeMap().get(this.dragState.nodeId);
      if (node) {
        this.canvasService.updateNodePosition(node.id, node.positionX, node.positionY).subscribe();
      }
    }

    this.dragState = null;
  }

  // ── Edge events ──────────────────────────────────────────────────────────

  onEdgeClick(event: MouseEvent, edge: CanvasEdgeModel): void {
    event.stopPropagation();
    if (edge.edgeType !== 'custom') return;

    this.canvasService.removeEdge(edge.id).subscribe({
      next: () => this.#localEdges.update(edges => edges.filter(e => e.id !== edge.id)),
      error: () =>
        this.snackBar.open('Errore durante la rimozione del collegamento', 'Chiudi', { duration: 3000 }),
    });
  }

  // ── Utilities ────────────────────────────────────────────────────────────

  truncate(label: string, max: number): string {
    return label.length <= max ? label : label.slice(0, max - 1) + '…';
  }
}

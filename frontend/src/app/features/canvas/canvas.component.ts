import {
  Component,
  inject,
  signal,
  computed,
  effect,
  ElementRef,
  ViewChild,
  OnDestroy,
  AfterViewInit,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Network, DataSet, Options } from 'vis-network/standalone';

import { CanvasService } from '../../services/canvas.service';
import { PlaylistService } from '../../services/playlist.service';
import { CanvasNodeModel, CanvasEdgeModel, CanvasState } from '../../models/canvas.model';
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
  ],
  templateUrl: './canvas.component.html',
  styleUrl: './canvas.component.scss',
})
export class CanvasComponent implements AfterViewInit, OnDestroy {
  @ViewChild('visContainer') visContainer!: ElementRef<HTMLDivElement>;

  private readonly canvasService = inject(CanvasService);
  private readonly playlistService = inject(PlaylistService);
  private readonly snackBar = inject(MatSnackBar);

  // ── Vis-Network instance ─────────────────────────────────────────────────
  private network: Network | null = null;
  private readonly visNodes = new DataSet<any>();
  private readonly visEdges = new DataSet<any>();

  // ── Local state mirrors (for sidebar logic) ──────────────────────────────
  readonly #localNodes = signal<CanvasNodeModel[]>([]);
  readonly #localEdges = signal<CanvasEdgeModel[]>([]);

  readonly onCanvasPlaylistIds = computed(
    () => new Set(this.#localNodes().filter(n => n.nodeType === 'playlist').map(n => n.referenceId))
  );

  readonly isLoading = this.canvasService.canvasResource.isLoading;

  // ── Connect mode ─────────────────────────────────────────────────────────
  readonly connectMode = signal(false);
  readonly hasCustomEdges = computed(() => this.#localEdges().some(e => e.edgeType === 'custom'));
  private pendingSourceId: number | null = null;

  // ── Library sidebar ──────────────────────────────────────────────────────
  readonly history = this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;

  constructor() {
    effect(() => {
      const val = this.canvasService.canvasResource.value();
      if (val) this.applyState(val);
    });
  }

  ngAfterViewInit(): void {
    this.initNetwork();
  }

  ngOnDestroy(): void {
    this.network?.destroy();
  }

  // ── Vis-Network init ─────────────────────────────────────────────────────

  private initNetwork(): void {
    const options: Options = {
      physics: {
        enabled: true,
        solver: 'forceAtlas2Based',
        forceAtlas2Based: {
          gravitationalConstant: -60,
          centralGravity: 0.008,
          springLength: 120,
          springConstant: 0.04,
          damping: 0.5,
        },
        stabilization: { iterations: 150, fit: true },
      },
      interaction: {
        hover: true,
        tooltipDelay: 200,
        multiselect: false,
      },
      edges: {
        smooth: { enabled: true, type: 'dynamic', roundness: 0.4 },
        color: { inherit: false },
      },
      nodes: {
        shape: 'dot',
        font: { color: '#cccccc', size: 11, face: 'Inter, sans-serif' },
        borderWidth: 0,
        shadow: { enabled: true, color: 'rgba(0,0,0,0.5)', size: 8, x: 2, y: 2 },
      },
    };

    this.network = new Network(
      this.visContainer.nativeElement,
      { nodes: this.visNodes, edges: this.visEdges },
      options
    );

    // Save positions after drag
    this.network.on('dragEnd', (params) => {
      if (!params.nodes.length) return;
      const nodeId: number = params.nodes[0];
      const pos = this.network!.getPosition(nodeId);
      this.canvasService.updateNodePosition(nodeId, pos.x, pos.y).subscribe();
    });

    // Handle node click for connect mode
    this.network.on('click', (params) => {
      if (!this.connectMode()) return;
      if (!params.nodes.length) return;

      const clickedId: number = params.nodes[0];

      if (this.pendingSourceId === null) {
        this.pendingSourceId = clickedId;
        this.visNodes.update({ id: clickedId, borderWidth: 3, color: { border: '#ffffff' } });
        return;
      }

      if (this.pendingSourceId === clickedId) {
        this.visNodes.update({ id: clickedId, borderWidth: 0, color: { border: 'transparent' } });
        this.pendingSourceId = null;
        return;
      }

      const sourceId = this.pendingSourceId;
      this.visNodes.update({ id: sourceId, borderWidth: 0, color: { border: 'transparent' } });
      this.pendingSourceId = null;

      this.canvasService.createEdge(sourceId, clickedId).subscribe({
        next: (edge) => {
          this.#localEdges.update(edges => [...edges, edge]);
          this.visEdges.add(this.mapEdgeToVis(edge));
        },
        error: (err) =>
          this.snackBar.open(err.error ?? 'Errore durante il collegamento', 'Chiudi', { duration: 3000 }),
      });
    });

    // Handle edge click to delete custom edges
    this.network.on('selectEdge', (params) => {
      if (this.connectMode()) return;
      if (!params.edges.length) return;

      const edgeId: number = params.edges[0];
      const edge = this.#localEdges().find(e => e.id === edgeId);
      if (!edge || edge.edgeType !== 'custom') {
        this.network!.unselectAll();
        return;
      }

      this.canvasService.removeEdge(edgeId).subscribe({
        next: () => {
          this.#localEdges.update(edges => edges.filter(e => e.id !== edgeId));
          this.visEdges.remove(edgeId);
        },
        error: () =>
          this.snackBar.open('Errore durante la rimozione del collegamento', 'Chiudi', { duration: 3000 }),
      });
    });
  }

  // ── State sync ────────────────────────────────────────────────────────────

  private applyState(state: CanvasState): void {
    this.#localNodes.set(state.nodes);
    this.#localEdges.set(state.edges);

    this.visNodes.clear();
    this.visEdges.clear();
    this.visNodes.add(state.nodes.map(n => this.mapNodeToVis(n)));
    this.visEdges.add(state.edges.map(e => this.mapEdgeToVis(e)));
  }

  private mapNodeToVis(n: CanvasNodeModel): any {
    const isPlaylist = n.nodeType === 'playlist';
    return {
      id: n.id,
      label: n.label,
      x: n.positionX,
      y: n.positionY,
      size: isPlaylist ? 22 : 10,
      color: {
        background: n.color ?? '#1ed760',
        border: 'transparent',
        highlight: { background: n.color ?? '#1ed760', border: '#ffffff' },
        hover: { background: n.color ?? '#1ed760', border: '#ffffff' },
      },
      font: {
        size: isPlaylist ? 13 : 10,
        bold: isPlaylist,
        color: isPlaylist ? '#ffffff' : '#aaaaaa',
      },
      physics: true,
    };
  }

  private mapEdgeToVis(e: CanvasEdgeModel): any {
    const isCustom = e.edgeType === 'custom';
    return {
      id: e.id,
      from: e.sourceNodeId,
      to: e.targetNodeId,
      color: isCustom ? { color: '#1ed760', opacity: 0.9 } : { color: '#ffffff', opacity: 0.08 },
      width: isCustom ? 2 : 1,
      dashes: isCustom ? [6, 4] : false,
      selectable: isCustom,
      chosen: isCustom,
    };
  }

  // ── Sidebar helpers ───────────────────────────────────────────────────────

  isOnCanvas(spotifyId: string): boolean {
    return this.onCanvasPlaylistIds().has(spotifyId);
  }

  displayName(p: PlaylistSummary): string {
    return p.playlistName || (p as any).name || p.spotifyId;
  }

  getPlaylistColor(spotifyId: string): string {
    const node = this.#localNodes().find(n => n.nodeType === 'playlist' && n.referenceId === spotifyId);
    return node?.color ?? '#1ed760';
  }

  addToCanvas(spotifyId: string): void {
    this.canvasService.addPlaylist(spotifyId).subscribe({
      next: (state) => {
        this.applyState(state);
        const newNode = state.nodes.find(n => n.nodeType === 'playlist' && n.referenceId === spotifyId);
        if (newNode) {
          setTimeout(() => this.network?.focus(newNode.id, { scale: 0.8, animation: true }), 300);
        }
      },
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore durante l\'aggiunta', 'Chiudi', { duration: 3000 }),
    });
  }

  removeFromCanvas(spotifyId: string): void {
    this.canvasService.removePlaylist(spotifyId).subscribe({
      next: (state) => this.applyState(state),
      error: () =>
        this.snackBar.open('Errore durante la rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  // ── Connect mode ──────────────────────────────────────────────────────────

  toggleConnectMode(): void {
    const entering = !this.connectMode();
    this.connectMode.set(entering);
    this.pendingSourceId = null;

    // Disable physics drag-to-connect confusion: freeze nodes in connect mode
    this.network?.setOptions({ interaction: { dragNodes: !entering } });
  }
}

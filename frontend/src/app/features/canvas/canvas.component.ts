import {
  Component,
  inject,
  signal,
  computed,
  effect,
  ElementRef,
  viewChild,
  OnDestroy,
  AfterViewInit,
  ChangeDetectionStrategy,
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
import { CanvasNodeModel, CanvasEdgeModel, CanvasState } from '../../models/canvas.model';
import { PlaylistSummary, PlaylistResult, Track } from '../../models/playlist.model';

const NODE_WIDTH_PLAYLIST = 180;
const NODE_HEIGHT_PLAYLIST = 80;
const NODE_WIDTH_TRACK = 170;
const NODE_HEIGHT_TRACK = 48;

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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CanvasComponent implements AfterViewInit, OnDestroy {
  private readonly canvasService = inject(CanvasService);
  private readonly playlistService = inject(PlaylistService);
  private readonly snackBar = inject(MatSnackBar);

  readonly canvasArea = viewChild.required<ElementRef<HTMLDivElement>>('canvasArea');

  readonly nodes = signal<CanvasNodeModel[]>([]);
  readonly edges = signal<CanvasEdgeModel[]>([]);

  readonly panX = signal(0);
  readonly panY = signal(0);
  readonly zoom = signal(1);
  readonly transformStyle = computed(
    () => `translate(${this.panX()}px, ${this.panY()}px) scale(${this.zoom()})`
  );

  private isPanning = false;
  private panStartX = 0;
  private panStartY = 0;
  private panStartPanX = 0;
  private panStartPanY = 0;

  private dragNode: CanvasNodeModel | null = null;
  private dragOffsetX = 0;
  private dragOffsetY = 0;

  readonly bridgeMode = signal(false);
  readonly bridgeSource = signal<CanvasNodeModel | null>(null);
  readonly hasUnsavedChanges = signal(false);

  readonly screenEdges = computed(() => {
    const n = this.nodes();
    const z = this.zoom();
    const px = this.panX();
    const py = this.panY();
    const nodeMap = new Map(n.map(node => [node.id, node]));

    return this.edges().map(e => {
      const src = nodeMap.get(e.sourceNodeId);
      const tgt = nodeMap.get(e.targetNodeId);
      if (!src || !tgt) return null;

      const srcW = this.getNodeWidth(src);
      const srcH = this.getNodeHeight(src);
      const tgtW = this.getNodeWidth(tgt);
      const tgtH = this.getNodeHeight(tgt);

      const srcCx = src.positionX + srcW / 2;
      const srcCy = src.positionY + srcH / 2;
      const tgtCx = tgt.positionX + tgtW / 2;
      const tgtCy = tgt.positionY + tgtH / 2;

      const p1 = this.getRectEdgePoint(srcCx, srcCy, srcW, srcH, tgtCx, tgtCy);
      const p2 = this.getRectEdgePoint(tgtCx, tgtCy, tgtW, tgtH, srcCx, srcCy);

      return {
        id: e.id,
        x1: p1.x * z + px,
        y1: p1.y * z + py,
        x2: p2.x * z + px,
        y2: p2.y * z + py,
        sourceColor: src.color || '#ffffff',
        targetColor: tgt.color || '#ffffff',
      };
    }).filter(Boolean) as {
      id: number; x1: number; y1: number; x2: number; y2: number;
      sourceColor: string; targetColor: string;
    }[];
  });

  readonly isLoading = this.canvasService.canvasResource.isLoading;
  readonly history = this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;

  readonly expandedPlaylistId = signal<string | null>(null);
  readonly expandedPlaylistTracks = signal<Track[]>([]);
  readonly expandedPlaylistLoading = signal(false);

  readonly NODE_WIDTH_PLAYLIST = NODE_WIDTH_PLAYLIST;
  readonly NODE_HEIGHT_PLAYLIST = NODE_HEIGHT_PLAYLIST;
  readonly NODE_WIDTH_TRACK = NODE_WIDTH_TRACK;
  readonly NODE_HEIGHT_TRACK = NODE_HEIGHT_TRACK;

  constructor() {
    effect(() => {
      const val = this.canvasService.canvasResource.value();
      if (val) {
        this.nodes.set(val.nodes);
        this.edges.set(val.edges);
      }
    });
  }

  ngAfterViewInit(): void {
    const el = this.canvasArea().nativeElement;
    el.addEventListener('wheel', this.onWheel, { passive: false });
  }

  ngOnDestroy(): void {
    const el = this.canvasArea()?.nativeElement;
    if (el) el.removeEventListener('wheel', this.onWheel);
  }

  getNodeWidth(node: CanvasNodeModel): number {
    return node.nodeType === 'playlist' ? NODE_WIDTH_PLAYLIST : NODE_WIDTH_TRACK;
  }

  getNodeHeight(node: CanvasNodeModel): number {
    return node.nodeType === 'playlist' ? NODE_HEIGHT_PLAYLIST : NODE_HEIGHT_TRACK;
  }

  private getRectEdgePoint(
    cx: number, cy: number, w: number, h: number,
    targetX: number, targetY: number
  ): { x: number; y: number } {
    const dx = targetX - cx;
    const dy = targetY - cy;

    if (dx === 0 && dy === 0) return { x: cx, y: cy };

    const halfW = w / 2;
    const halfH = h / 2;
    const absDx = Math.abs(dx);
    const absDy = Math.abs(dy);

    let scale: number;
    if (absDx * halfH > absDy * halfW) {
      scale = halfW / absDx;
    } else {
      scale = halfH / absDy;
    }

    return { x: cx + dx * scale, y: cy + dy * scale };
  }

  readonly onWheel = (e: WheelEvent): void => {
    e.preventDefault();
    const factor = e.deltaY > 0 ? 0.92 : 1.08;
    const newZoom = Math.min(3, Math.max(0.15, this.zoom() * factor));

    const rect = this.canvasArea().nativeElement.getBoundingClientRect();
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    const scale = newZoom / this.zoom();
    this.panX.set(mx - scale * (mx - this.panX()));
    this.panY.set(my - scale * (my - this.panY()));
    this.zoom.set(newZoom);
  };

  onCanvasPointerDown(e: PointerEvent): void {
    if (e.button === 1 || (e.button === 0 && !(e.target as HTMLElement).closest('.canvas-node'))) {
      this.isPanning = true;
      this.panStartX = e.clientX;
      this.panStartY = e.clientY;
      this.panStartPanX = this.panX();
      this.panStartPanY = this.panY();
      (e.target as HTMLElement).setPointerCapture(e.pointerId);
      e.preventDefault();
    }
  }

  onCanvasPointerMove(e: PointerEvent): void {
    if (this.isPanning) {
      this.panX.set(this.panStartPanX + (e.clientX - this.panStartX));
      this.panY.set(this.panStartPanY + (e.clientY - this.panStartY));
      return;
    }

    if (this.dragNode) {
      const z = this.zoom();
      const rect = this.canvasArea().nativeElement.getBoundingClientRect();
      const worldX = (e.clientX - rect.left - this.panX()) / z;
      const worldY = (e.clientY - rect.top - this.panY()) / z;

      this.nodes.update(nodes =>
        nodes.map(n => n.id === this.dragNode!.id
          ? { ...n, positionX: worldX - this.dragOffsetX, positionY: worldY - this.dragOffsetY }
          : n
        )
      );
    }
  }

  onCanvasPointerUp(e: PointerEvent): void {
    if (this.isPanning) {
      this.isPanning = false;
      (e.target as HTMLElement).releasePointerCapture(e.pointerId);
      return;
    }

    if (this.dragNode) {
      this.dragNode = null;
      this.hasUnsavedChanges.set(true);
    }
  }

  onNodePointerDown(e: PointerEvent, node: CanvasNodeModel): void {
    if (this.bridgeMode()) return;
    e.stopPropagation();
    e.preventDefault();

    const z = this.zoom();
    const rect = this.canvasArea().nativeElement.getBoundingClientRect();
    const worldX = (e.clientX - rect.left - this.panX()) / z;
    const worldY = (e.clientY - rect.top - this.panY()) / z;

    this.dragNode = node;
    this.dragOffsetX = worldX - node.positionX;
    this.dragOffsetY = worldY - node.positionY;
  }

  toggleBridgeMode(): void {
    this.bridgeMode.update(v => !v);
    this.bridgeSource.set(null);
  }

  onNodeClick(node: CanvasNodeModel): void {
    if (!this.bridgeMode()) return;

    const src = this.bridgeSource();
    if (!src) {
      this.bridgeSource.set(node);
      return;
    }

    if (src.id === node.id) {
      this.bridgeSource.set(null);
      return;
    }

    this.canvasService.createEdge(src.id, node.id).subscribe({
      next: (edge) => {
        this.edges.update(edges => [...edges, edge]);
        this.bridgeSource.set(null);
        this.snackBar.open('Collegamento creato!', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nel creare il collegamento', 'Chiudi', { duration: 3000 }),
    });
  }

  onEdgeClick(edgeId: number): void {
    this.canvasService.removeEdge(edgeId).subscribe({
      next: () => {
        this.edges.update(edges => edges.filter(e => e.id !== edgeId));
        this.snackBar.open('Collegamento rimosso', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nella rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  onNodeRightClick(e: MouseEvent, node: CanvasNodeModel): void {
    e.preventDefault();
    this.canvasService.removeNode(node.id).subscribe({
      next: () => {
        this.nodes.update(nodes => nodes.filter(n => n.id !== node.id));
        this.edges.update(edges => edges.filter(
          edge => edge.sourceNodeId !== node.id && edge.targetNodeId !== node.id
        ));
        this.snackBar.open('Nodo rimosso', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nella rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  savePositions(): void {
    const items = this.nodes().map(n => ({
      id: n.id,
      positionX: n.positionX,
      positionY: n.positionY,
    }));

    this.canvasService.batchUpdatePositions(items).subscribe({
      next: () => {
        this.hasUnsavedChanges.set(false);
        this.snackBar.open('Posizioni salvate!', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nel salvataggio', 'Chiudi', { duration: 3000 }),
    });
  }

  clearCanvas(): void {
    if (!confirm('Sei sicuro? Verranno rimossi tutti i nodi e i collegamenti.')) return;

    this.canvasService.clearAll().subscribe({
      next: () => {
        this.nodes.set([]);
        this.edges.set([]);
        this.hasUnsavedChanges.set(false);
        this.snackBar.open('Canvas pulita!', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nella pulizia', 'Chiudi', { duration: 3000 }),
    });
  }

  addPlaylistToCanvas(spotifyId: string): void {
    this.canvasService.addPlaylistNode(spotifyId).subscribe({
      next: (node) => {
        this.nodes.update(nodes => [...nodes, node]);
        this.snackBar.open('Playlist aggiunta!', '', { duration: 1500 });
      },
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore', 'Chiudi', { duration: 3000 }),
    });
  }

  togglePlaylistExpand(playlist: PlaylistSummary): void {
    if (this.expandedPlaylistId() === playlist.spotifyId) {
      this.expandedPlaylistId.set(null);
      this.expandedPlaylistTracks.set([]);
      return;
    }

    this.expandedPlaylistId.set(playlist.spotifyId);
    this.expandedPlaylistLoading.set(true);

    this.playlistService.getPlaylistResult(playlist.spotifyId).subscribe({
      next: (result) => {
        this.expandedPlaylistTracks.set(result.tracks);
        this.expandedPlaylistLoading.set(false);
      },
      error: () => {
        this.expandedPlaylistTracks.set([]);
        this.expandedPlaylistLoading.set(false);
      },
    });
  }

  addTrackToCanvas(spotifyId: string, trackIndex: number): void {
    this.canvasService.addTrackNode(spotifyId, trackIndex).subscribe({
      next: (node) => {
        this.nodes.update(nodes => [...nodes, node]);
        this.snackBar.open('Brano aggiunto!', '', { duration: 1500 });
      },
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore', 'Chiudi', { duration: 3000 }),
    });
  }

  isNodeOnCanvas(referenceId: string): boolean {
    return this.nodes().some(n => n.referenceId === referenceId);
  }

  removeNodeByReferenceId(referenceId: string): void {
    const node = this.nodes().find(n => n.referenceId === referenceId);
    if (!node) return;
    this.canvasService.removeNode(node.id).subscribe({
      next: () => {
        this.nodes.update(nodes => nodes.filter(n => n.id !== node.id));
        this.edges.update(edges => edges.filter(
          edge => edge.sourceNodeId !== node.id && edge.targetNodeId !== node.id
        ));
        this.snackBar.open('Nodo rimosso', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nella rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  displayName(p: PlaylistSummary): string {
    return p.playlistName || (p as any).name || p.spotifyId;
  }

  trackById(_: number, node: CanvasNodeModel): number {
    return node.id;
  }

  trackEdgeById(_: number, edge: { id: number }): number {
    return edge.id;
  }
}

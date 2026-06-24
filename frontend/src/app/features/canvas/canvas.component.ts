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

import {
  forceSimulation,
  forceCollide,
  forceManyBody,
  forceX,
  forceY,
  type Simulation,
  type SimulationNodeDatum,
  type SimulationLinkDatum,
} from 'd3-force';

import { CanvasService } from '../../services/canvas.service';
import { PlaylistService } from '../../services/playlist.service';
import { CanvasNodeModel, CanvasEdgeModel, CanvasState } from '../../models/canvas.model';
import { PlaylistSummary } from '../../models/playlist.model';

// ── D3 simulation types ──────────────────────────────────────────

interface SimNode extends SimulationNodeDatum {
  id: number;
  label: string;
  artist?: string;
  color?: string;
  parentPlaylistId?: string;
  parentPlaylistName?: string;
  nodeType: string;
  referenceId: string;
  cluster: string;
}

interface SimLink extends SimulationLinkDatum<SimNode> {
  edgeId: number;
}

// ── Constants ─────────────────────────────────────────────────────

const NODE_WIDTH = 170;
const NODE_HEIGHT = 48;
const COLLISION_RADIUS = 92; // just enough to prevent overlap (half node width + tiny gap)
const CLUSTER_STRENGTH = 0.7; // strong pull toward cluster center
const CHARGE_STRENGTH = -8; // very light repulsion (tight packing)
const BRIDGE_SNAP_DISTANCE = 110; // how close bridged nodes get to each other

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

  // ── Simulation state
  private simulation: Simulation<SimNode, SimLink> | null = null;
  private simNodes: SimNode[] = [];
  private simLinks: SimLink[] = [];
  private simulationSettled = false;

  // ── Reactive signals for template
  readonly nodes = signal<SimNode[]>([]);
  readonly edges = signal<CanvasEdgeModel[]>([]);
  readonly edgeLines = signal<{ id: number; x1: number; y1: number; x2: number; y2: number }[]>([]);

  // ── Zoom / Pan
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

  // ── Drag
  private dragNode: SimNode | null = null;
  private dragOffsetX = 0;
  private dragOffsetY = 0;

  // ── Bridge mode
  readonly bridgeMode = signal(false);
  readonly bridgeSource = signal<SimNode | null>(null);
  readonly hasUnsavedChanges = signal(false);

  readonly onCanvasPlaylistIds = computed(
    () => new Set(
      this.nodes()
        .map(b => b.parentPlaylistId)
        .filter((id): id is string => !!id)
    )
  );

  readonly isLoading = this.canvasService.canvasResource.isLoading;

  // ── Library sidebar
  readonly history = this.playlistService.historyResource.value;
  readonly historyLoading = this.playlistService.historyResource.isLoading;

  // ── Cluster labels (computed from node positions)
  readonly clusterLabels = computed(() => {
    const n = this.nodes();
    const clusters = new Map<string, { name: string; color: string; sumX: number; sumY: number; count: number }>();

    for (const node of n) {
      if (!node.cluster) continue;
      const existing = clusters.get(node.cluster);
      if (existing) {
        existing.sumX += node.x ?? 0;
        existing.sumY += node.y ?? 0;
        existing.count++;
      } else {
        clusters.set(node.cluster, {
          name: node.parentPlaylistName ?? node.cluster,
          color: node.color ?? '#1ed760',
          sumX: node.x ?? 0,
          sumY: node.y ?? 0,
          count: 1,
        });
      }
    }

    return [...clusters.values()].map(c => ({
      name: c.name,
      color: c.color,
      x: c.sumX / c.count,
      y: c.sumY / c.count - (NODE_HEIGHT + 18),
    }));
  });

  readonly NODE_WIDTH = NODE_WIDTH;
  readonly NODE_HEIGHT = NODE_HEIGHT;

  constructor() {
    effect(() => {
      const val = this.canvasService.canvasResource.value();
      if (val) this.initSimulation(val);
    });
  }

  ngAfterViewInit(): void {
    const el = this.canvasArea().nativeElement;
    el.addEventListener('wheel', this.onWheel, { passive: false });
  }

  ngOnDestroy(): void {
    if (this.simulation) this.simulation.stop();
    const el = this.canvasArea()?.nativeElement;
    if (el) el.removeEventListener('wheel', this.onWheel);
  }

  // ── Simulation ─────────────────────────────────────────────────

  private initSimulation(state: CanvasState): void {
    if (this.simulation) this.simulation.stop();
    this.simulationSettled = false;

    const clusterCenters = this.computeClusterCenters(state.nodes);

    // Check if nodes already have meaningful positions (loaded from DB)
    const hasPositions = state.nodes.some(n => n.positionX !== 0 || n.positionY !== 0);

    this.simNodes = state.nodes.map(n => ({
      id: n.id,
      label: n.label,
      artist: n.artist,
      color: n.color,
      parentPlaylistId: n.parentPlaylistId,
      parentPlaylistName: n.parentPlaylistName,
      nodeType: n.nodeType,
      referenceId: n.referenceId,
      cluster: n.parentPlaylistId ?? '',
      x: n.positionX,
      y: n.positionY,
    }));

    this.simLinks = state.edges.map(e => ({
      edgeId: e.id,
      source: this.simNodes.find(n => n.id === e.sourceNodeId)!,
      target: this.simNodes.find(n => n.id === e.targetNodeId)!,
    })).filter(l => l.source && l.target);

    this.edges.set(state.edges);

    // If nodes already have saved positions, skip simulation and freeze immediately
    if (hasPositions) {
      this.freezeAllNodes();
      this.simulationSettled = true;
      this.refreshView();
      return;
    }

    // Run simulation only for initial layout, then freeze
    this.simulation = forceSimulation<SimNode>(this.simNodes)
      .force('collide', forceCollide<SimNode>(COLLISION_RADIUS).strength(0.8))
      .force('charge', forceManyBody<SimNode>().strength(CHARGE_STRENGTH))
      .force('clusterX', forceX<SimNode>(d => clusterCenters.get(d.cluster)?.x ?? 0).strength(CLUSTER_STRENGTH))
      .force('clusterY', forceY<SimNode>(d => clusterCenters.get(d.cluster)?.y ?? 0).strength(CLUSTER_STRENGTH))
      .alphaDecay(0.05)
      .on('tick', () => this.onTick())
      .on('end', () => this.onSimulationEnd());

    this.simulation.alpha(1).restart();
  }

  /** Called when the simulation naturally settles */
  private onSimulationEnd(): void {
    this.freezeAllNodes();
    this.simulationSettled = true;
    this.hasUnsavedChanges.set(true);
    this.refreshView();
  }

  /** Pin all nodes at their current position so nothing moves */
  private freezeAllNodes(): void {
    for (const node of this.simNodes) {
      node.fx = node.x;
      node.fy = node.y;
    }
  }

  /** Push current state to signals for Angular rendering */
  private refreshView(): void {
    this.nodes.set([...this.simNodes]);
    this.updateEdgeLines();
  }

  private computeClusterCenters(nodes: CanvasNodeModel[]): Map<string, { x: number; y: number }> {
    const clusters = new Map<string, { x: number; y: number }>();
    const playlists = [...new Set(nodes.map(n => n.parentPlaylistId).filter(Boolean))] as string[];
    const spacing = 500;
    const cols = Math.ceil(Math.sqrt(playlists.length));

    playlists.forEach((pid, i) => {
      clusters.set(pid, {
        x: (i % cols) * spacing + 300,
        y: Math.floor(i / cols) * spacing + 300,
      });
    });

    return clusters;
  }

  private onTick(): void {
    this.nodes.set([...this.simNodes]);
    this.updateEdgeLines();
  }

  private updateEdgeLines(): void {
    this.edgeLines.set(this.simLinks.map(l => {
      const s = l.source as SimNode;
      const t = l.target as SimNode;
      return {
        id: l.edgeId,
        x1: s.fx ?? s.x ?? 0,
        y1: s.fy ?? s.y ?? 0,
        x2: t.fx ?? t.x ?? 0,
        y2: t.fy ?? t.y ?? 0,
      };
    }));
  }

  // ── Zoom / Pan ─────────────────────────────────────────────────

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
    if (e.button === 1 || (e.button === 0 && !(e.target as HTMLElement).closest('.track-block'))) {
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

      // Direct position update — no simulation restart
      this.dragNode.fx = worldX - this.dragOffsetX;
      this.dragNode.fy = worldY - this.dragOffsetY;
      this.dragNode.x = this.dragNode.fx;
      this.dragNode.y = this.dragNode.fy;
      this.refreshView();
    }
  }

  onCanvasPointerUp(e: PointerEvent): void {
    if (this.isPanning) {
      this.isPanning = false;
      (e.target as HTMLElement).releasePointerCapture(e.pointerId);
      return;
    }

    if (this.dragNode) {
      // Node stays pinned where dropped
      this.dragNode = null;
      this.hasUnsavedChanges.set(true);
      this.refreshView();
    }
  }

  // ── Node Drag ──────────────────────────────────────────────────

  onNodePointerDown(e: PointerEvent, node: SimNode): void {
    if (this.bridgeMode()) return;
    e.stopPropagation();
    e.preventDefault();

    const z = this.zoom();
    const rect = this.canvasArea().nativeElement.getBoundingClientRect();
    const worldX = (e.clientX - rect.left - this.panX()) / z;
    const worldY = (e.clientY - rect.top - this.panY()) / z;

    this.dragNode = node;
    this.dragOffsetX = worldX - (node.fx ?? node.x ?? 0);
    this.dragOffsetY = worldY - (node.fy ?? node.y ?? 0);
  }

  // ── Bridge ─────────────────────────────────────────────────────

  toggleBridgeMode(): void {
    this.bridgeMode.update(v => !v);
    this.bridgeSource.set(null);
  }

  onNodeClick(node: SimNode): void {
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
        const sourceNode = this.simNodes.find(n => n.id === edge.sourceNodeId)!;
        const targetNode = this.simNodes.find(n => n.id === edge.targetNodeId)!;
        const newLink: SimLink = {
          edgeId: edge.id,
          source: sourceNode,
          target: targetNode,
        };
        this.simLinks.push(newLink);

        // Snap bridged nodes toward each other
        this.snapBridgedNodes(sourceNode, targetNode);

        this.bridgeSource.set(null);
        this.hasUnsavedChanges.set(true);
        this.refreshView();
        this.snackBar.open('Ponte creato!', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nel creare il ponte', 'Chiudi', { duration: 3000 }),
    });
  }

  onEdgeClick(edgeId: number): void {
    this.canvasService.removeEdge(edgeId).subscribe({
      next: () => {
        this.edges.update(edges => edges.filter(e => e.id !== edgeId));
        const idx = this.simLinks.findIndex(l => l.edgeId === edgeId);
        if (idx >= 0) this.simLinks.splice(idx, 1);
        this.refreshView();
        this.snackBar.open('Ponte rimosso', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nella rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  /** Move two bridged nodes toward each other so they "attach" */
  private snapBridgedNodes(a: SimNode, b: SimNode): void {
    const ax = a.fx ?? a.x ?? 0;
    const ay = a.fy ?? a.y ?? 0;
    const bx = b.fx ?? b.x ?? 0;
    const by = b.fy ?? b.y ?? 0;

    const dx = bx - ax;
    const dy = by - ay;
    const dist = Math.sqrt(dx * dx + dy * dy);

    if (dist <= BRIDGE_SNAP_DISTANCE) return; // already close enough

    // Move each node halfway toward the target snap distance
    const targetDist = BRIDGE_SNAP_DISTANCE;
    const moveRatio = (dist - targetDist) / (2 * dist);

    a.fx = ax + dx * moveRatio;
    a.fy = ay + dy * moveRatio;
    a.x = a.fx;
    a.y = a.fy;

    b.fx = bx - dx * moveRatio;
    b.fy = by - dy * moveRatio;
    b.x = b.fx;
    b.y = b.fy;
  }

  // ── Save ────────────────────────────────────────────────────────

  savePositions(): void {
    const items = this.simNodes.map(n => ({
      id: n.id,
      positionX: n.x ?? 0,
      positionY: n.y ?? 0,
    }));

    this.canvasService.batchUpdatePositions(items).subscribe({
      next: () => {
        this.hasUnsavedChanges.set(false);
        this.snackBar.open('Posizioni salvate!', '', { duration: 1500 });
      },
      error: () => this.snackBar.open('Errore nel salvataggio', 'Chiudi', { duration: 3000 }),
    });
  }

  // ── Sidebar helpers ────────────────────────────────────────────

  isOnCanvas(spotifyId: string): boolean {
    return this.onCanvasPlaylistIds().has(spotifyId);
  }

  displayName(p: PlaylistSummary): string {
    return p.playlistName || (p as any).name || p.spotifyId;
  }

  getPlaylistColor(spotifyId: string): string {
    const node = this.nodes().find(b => b.parentPlaylistId === spotifyId);
    return node?.color ?? '#1ed760';
  }

  addToCanvas(spotifyId: string): void {
    this.canvasService.addPlaylistNode(spotifyId).subscribe({
      next: (state: CanvasState) => this.initSimulation(state),
      error: (err) =>
        this.snackBar.open(err.error ?? 'Errore durante l\'aggiunta', 'Chiudi', { duration: 3000 }),
    });
  }

  removeFromCanvas(spotifyId: string): void {
    this.canvasService.removePlaylistNode(spotifyId).subscribe({
      next: () => {
        const removedIds = new Set(
          this.simNodes.filter(n => n.parentPlaylistId === spotifyId).map(n => n.id)
        );
        this.simNodes = this.simNodes.filter(n => n.parentPlaylistId !== spotifyId);
        this.simLinks = this.simLinks.filter(l => {
          const s = l.source as SimNode;
          const t = l.target as SimNode;
          return s.parentPlaylistId !== spotifyId && t.parentPlaylistId !== spotifyId;
        });
        this.edges.update(edges =>
          edges.filter(e => !removedIds.has(e.sourceNodeId) && !removedIds.has(e.targetNodeId))
        );
        this.refreshView();
      },
      error: () =>
        this.snackBar.open('Errore durante la rimozione', 'Chiudi', { duration: 3000 }),
    });
  }

  trackById(_: number, node: SimNode): number {
    return node.id;
  }

  trackEdgeById(_: number, edge: { id: number }): number {
    return edge.id;
  }
}

export interface Track {
  name: string;
  artists: string[];
  durationMs?: number;
  previewUrl?: string;
}

export interface PlaylistResult {
  playlistName: string;
  description?: string;
  totalTracks: number;
  imageUrl?: string;
  tracks: Track[];
  genres: string[];
}

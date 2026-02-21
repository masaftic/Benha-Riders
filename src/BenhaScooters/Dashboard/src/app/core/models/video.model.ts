export interface Video {
  id: string;
  title: string;
  description: string | null;
  durationSeconds: number;
  videoUrl: string;
  thumbnailUrl: string;
  tagId: number;
  tagName?: string;
  uploadedAt: string;
  isVisibleToGuests: boolean;
  canGuestsPlay: boolean;
}

export interface CreateVideoDto {
  title: string;
  description: string | null;
  durationSeconds: number;
  videoFile: File;
  thumbnailFile: File;
  tagId: number;
  isVisibleToGuests: boolean;
  canGuestsPlay: boolean;
}

export interface UpdateVideoDto {
  title: string;
  description: string | null;
  durationSeconds: number;
  videoFile?: File;
  thumbnailFile?: File;
  tagId: number;
  isVisibleToGuests: boolean;
  canGuestsPlay: boolean;
}

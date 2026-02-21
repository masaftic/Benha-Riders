export interface Tag {
  id: number;
  name: string;
  videoCount: number;
}

export interface CreateTagDto {
  name: string;
}

export interface UpdateTagDto {
  name: string;
}


export interface NewsItem {
  id: number;
  title: string;
  subtitle: string;
  imageUrl: string;
  createdAt: string; // ISO date string
  updatedAt: string | null; // ISO date string or null
}


export interface CreateNewsDto {
  title: string;
  subtitle: string;
  image: File;
}

export interface UpdateNewsDto {
  title: string;
  subtitle: string;
  image?: File;
}

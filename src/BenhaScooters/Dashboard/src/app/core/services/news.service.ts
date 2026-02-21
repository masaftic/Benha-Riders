import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Video, CreateVideoDto, UpdateVideoDto } from '../models/video.model';
import { PaginationRequest, PaginatedResponse } from '../models/pagination.model';
import { environment } from '../../../environments/environment';
import { CreateNewsDto, NewsItem, UpdateNewsDto } from '../models/news.model';

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/news`;

  getNews(request: PaginationRequest): Observable<PaginatedResponse<NewsItem>> {
    let params = new HttpParams();

    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request.search) params = params.set('search', request.search);

    return this.http.get<PaginatedResponse<NewsItem>>(this.apiUrl, { params });
  }

  getNewsById(id: number): Observable<NewsItem> {
    return this.http.get<NewsItem>(`${this.apiUrl}/${id}`);
  }

  createNews(dto: CreateNewsDto): Observable<NewsItem> {
    const formData = new FormData();
    formData.append('title', dto.title);
    formData.append('subtitle', dto.subtitle);
    formData.append('image', dto.image);

    return this.http.post<NewsItem>(`${this.apiUrl}`, formData);
  }

  updateNews(id: number, dto: UpdateNewsDto): Observable<NewsItem> {
    const formData = new FormData();
    formData.append('title', dto.title);
    formData.append('subtitle', dto.subtitle);
    if (dto.image) {
      formData.append('image', dto.image);
    }

    return this.http.put<NewsItem>(`${this.apiUrl}/${id}`, formData);
  }

  deleteNews(id: number): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${id}`);
  }
}

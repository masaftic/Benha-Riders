import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Video, CreateVideoDto, UpdateVideoDto } from '../models/video.model';
import { PaginationRequest, PaginatedResponse } from '../models/pagination.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class VideoService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/videos`;

  getVideos(request: PaginationRequest & { tagId?: number }): Observable<PaginatedResponse<Video>> {
    let params = new HttpParams();

    if (request.pageNumber) params = params.set('pageNumber', request.pageNumber.toString());
    if (request.pageSize) params = params.set('pageSize', request.pageSize.toString());
    if (request.search) params = params.set('search', request.search);
    if (request.orderBy) params = params.set('orderBy', request.orderBy);
    if (request.isDescending !== undefined) params = params.set('isDescending', request.isDescending.toString());
    if (request.tagId) params = params.set('tagId', request.tagId.toString());

    return this.http.get<PaginatedResponse<Video>>(this.apiUrl, { params });
  }

  getVideoById(id: string): Observable<Video> {
    return this.http.get<Video>(`${this.apiUrl}/${id}`);
  }

  createVideo(dto: CreateVideoDto): Observable<Video> {
    const formData = new FormData();
    formData.append('title', dto.title);
    if (dto.description) {
      formData.append('description', dto.description);
    }
    formData.append('durationSeconds', dto.durationSeconds.toString());
    formData.append('videoFile', dto.videoFile);
    formData.append('thumbnailFile', dto.thumbnailFile);
    formData.append('tagId', dto.tagId.toString());
    formData.append('isVisibleToGuests', dto.isVisibleToGuests.toString());
    formData.append('canGuestsPlay', dto.canGuestsPlay.toString());

    return this.http.post<Video>(`${this.apiUrl}/admin`, formData);
  }

  updateVideo(id: string, dto: UpdateVideoDto): Observable<Video> {
    const formData = new FormData();
    formData.append('title', dto.title);
    if (dto.description) {
      formData.append('description', dto.description);
    }
    formData.append('durationSeconds', dto.durationSeconds.toString());
    if (dto.videoFile) {
      formData.append('videoFile', dto.videoFile);
    }
    if (dto.thumbnailFile) {
      formData.append('thumbnailFile', dto.thumbnailFile);
    }
    formData.append('tagId', dto.tagId.toString());
    formData.append('isVisibleToGuests', dto.isVisibleToGuests.toString());
    formData.append('canGuestsPlay', dto.canGuestsPlay.toString());

    return this.http.put<Video>(`${this.apiUrl}/admin/${id}`, formData);
  }

  deleteVideo(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/admin/${id}`);
  }
}

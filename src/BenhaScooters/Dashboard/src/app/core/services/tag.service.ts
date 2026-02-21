import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tag, CreateTagDto, UpdateTagDto } from '../models/tag.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TagService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/tags`;

  getTags(): Observable<Tag[]> {
    return this.http.get<Tag[]>(this.apiUrl);
  }

  createTag(dto: CreateTagDto): Observable<Tag> {
    return this.http.post<Tag>(this.apiUrl, dto);
  }

  updateTag(id: number, dto: UpdateTagDto): Observable<Tag> {
    return this.http.put<Tag>(`${this.apiUrl}/${id}`, dto);
  }

  deleteTag(id: number): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${id}`);
  }
}

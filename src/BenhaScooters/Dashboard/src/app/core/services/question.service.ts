import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Question, CreateQuestionDto, UpdateQuestionDto } from '../models/question.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class QuestionService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/questions`;

  getQuestionsByVideoId(videoId?: string): Observable<Question[]> {
    return this.http.get<Question[]>(`${this.apiUrl}/admin?videoId=${videoId}`);
  }

  getQuestionById(id: number): Observable<Question> {
    return this.http.get<Question>(`${this.apiUrl}/${id}`);
  }

  createQuestion(dto: CreateQuestionDto): Observable<Question> {
    return this.http.post<Question>(this.apiUrl, dto);
  }

  updateQuestion(id: number, dto: UpdateQuestionDto): Observable<Question> {
    return this.http.put<Question>(`${this.apiUrl}/${id}`, dto);
  }

  deleteQuestion(id: number): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${this.apiUrl}/${id}`);
  }
}

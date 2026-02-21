import { Component, computed, effect, inject, model, signal } from '@angular/core';
import { ButtonModule } from "primeng/button";
import { SelectModule } from "primeng/select";
import { DataViewLazyLoadEvent, DataViewModule } from "primeng/dataview";
import { ToastModule } from "primeng/toast";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { Tag } from 'primeng/tag';
import { Video } from '../../../../core/models/video.model';
import { debounceSignal } from '../../../../core/utils';
import { NewsCard } from "./news-card/news-card";
import { CreateNewsDto, NewsItem, UpdateNewsDto } from '../../../../core/models/news.model';
import { toObservable } from '@angular/core/rxjs-interop';
import { tap, switchMap, catchError, of } from 'rxjs';
import { NewsService } from '../../../../core/services/news.service';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Dialog } from "primeng/dialog";
import { NewsForm } from "./news-form/news-form";
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-news',
  imports: [ButtonModule, SelectModule, DataViewModule, ToastModule, ConfirmDialogModule, NewsCard, Dialog, NewsForm, InputTextModule],
  providers: [MessageService, ConfirmationService],
  templateUrl: './news.html',
  styleUrl: './news.scss',
})
export class News {
  selectedNewsItem = signal<NewsItem | null>(null);
  isDialogVisible = model<boolean>(false);

  news = signal<NewsItem[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  searchInputVal = signal<string>('');
  searchQuery = debounceSignal(this.searchInputVal, 500, '');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(8);
  reloadTrigger = signal<number>(0);

  query = computed(() => ({
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    search: this.searchQuery(),
    reloadTrigger: this.reloadTrigger()
  }));

  private readonly newsService = inject(NewsService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);


  constructor() {
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(query =>
        this.newsService.getNews(query).pipe(
          catchError(() => {
            this.loading.set(false);
            return of(null);
          })
        )
      )
    ).subscribe(response => {
      if (!response) return;
      console.log(response);

      this.news.set(response.items);
      this.totalRecords.set(response.totalCount);
      this.loading.set(false);
    });
  }

  onSearchChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputVal.set(value);
    this.pageNumber.set(1);
  }

  onPageChange(event: DataViewLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  createNews() {
    this.selectedNewsItem.set(null);
    this.isDialogVisible.set(true);
  }

  deleteNews(newsId: number) {
    this.confirmationService.confirm({
      message: 'Are you sure you want to delete this news item?',
      header: 'Confirm Deletion',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.confirmDelete(newsId);
      }
    });
  }

  private confirmDelete(newsId: number) {
    this.newsService.deleteNews(newsId).subscribe({
      next: (res) => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'News item deleted successfully.' });
        this.reloadTrigger.update(n => n + 1);
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete news item.' });
      }
    });
  }

  editNews(newsItem: NewsItem) {
    this.selectedNewsItem.set(newsItem);
    this.isDialogVisible.set(true);
  }

  onDialogSave(item: CreateNewsDto | UpdateNewsDto) {
    if (this.selectedNewsItem()) {
      // Update existing news item
      this.newsService.updateNews(this.selectedNewsItem()!.id, item as UpdateNewsDto).subscribe({
        next: (updatedItem) => {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'News item updated successfully.' });
          this.isDialogVisible.set(false);
          this.reloadTrigger.update(n => n + 1);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update news item.' });
        }
      });
    } else {
      // Create new news item
      this.newsService.createNews(item as CreateNewsDto).subscribe({
        next: (createdItem) => {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'News item created successfully.' });
          this.isDialogVisible.set(false);
          this.reloadTrigger.update(n => n + 1);
        },
        error: () => {
          this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to create news item.' });
        }
      });
    }
  }
}

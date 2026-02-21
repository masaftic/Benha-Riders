import { Component, inject, signal, OnInit, ChangeDetectionStrategy, effect, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule, NgFor } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DataView, DataViewModule, DataViewLazyLoadEvent } from 'primeng/dataview';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { VideoService } from '../../../../core/services/video.service';
import { TagService } from '../../../../core/services/tag.service';
import { Video } from '../../../../core/models/video.model';
import { Tag } from '../../../../core/models/tag.model';
import { VideoCardComponent } from './video-card.component';
import { debounceSignal } from '../../../../core/utils';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-videos',
  imports: [
    CommonModule,
    FormsModule,
    DataView,
    DataViewModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    Select,
    VideoCardComponent,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './videos.component.html',
  styleUrl: './videos.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VideosComponent implements OnInit {
  private readonly videoService = inject(VideoService);
  private readonly tagService = inject(TagService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  videos = signal<Video[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  tags = signal<Tag[]>([]);
  searchInputVal = signal<string>('');
  searchQuery = debounceSignal(this.searchInputVal, 500, '');
  selectedTagId = signal<number | null>(null);
  pageNumber = signal<number>(1);
  pageSize = signal<number>(8);
  reloadTrigger = signal<number>(0);

  query = computed(() => ({
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    search: this.searchQuery(),
    tagId: this.selectedTagId() ?? undefined,
    orderBy: 'uploadedAt',
    isDescending: true,
    reload: this.reloadTrigger()
  }));

  constructor() {
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(query =>
        this.videoService.getVideos(query).pipe(
          catchError(() => {
            this.loading.set(false);
            return of(null);
          })
        )
      )
    ).subscribe(response => {
      if (!response) return;

      this.videos.set(response.items);
      this.totalRecords.set(response.totalCount);
      this.loading.set(false);
    });
  }

  ngOnInit(): void {
    this.loadTags();
  }

  loadTags(): void {
    this.tagService.getTags().subscribe({
      next: (tags) => this.tags.set(tags)
    });
  }

  onSearchChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputVal.set(value);
    this.pageNumber.set(1);
  }

  onTagFilterChange(tagId: number | null): void {
    this.selectedTagId.set(tagId);
    this.pageNumber.set(1);
  }

  onPageChange(event: DataViewLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  createVideo(): void {
    this.router.navigate(['/dashboard/videos/create']);
  }

  editVideo(id: string): void {
    this.router.navigate(['/dashboard/videos/edit', id]);
  }

  confirmDelete(video: Video): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${video.title}"?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deleteVideo(video.id)
    });
  }

  deleteVideo(id: string): void {
    this.videoService.deleteVideo(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Video deleted successfully'
        });

        // Trigger reload by updating a signal in the query
        this.reloadTrigger.update(n => n + 1);
      }
    });
  }
}

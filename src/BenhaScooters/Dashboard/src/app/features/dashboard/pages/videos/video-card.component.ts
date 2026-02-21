import { Component, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { Video } from '../../../../core/models/video.model';

@Component({
  selector: 'app-video-card',
  imports: [ButtonModule, TagModule],
  template: `
    <div class="bg-white shadow-sm border-1 border-gray-200 rounded-lg overflow-hidden flex flex-col h-full">
      <!-- Thumbnail -->
      <div class="relative flex-shrink-0">
        <img
          [src]="video().thumbnailUrl"
          [alt]="video().title"
          class="w-full h-48 object-cover"
        />
        @if (video().tagName) {
          <div class="top-2 right-2 absolute">
            <p-tag [value]="video().tagName" severity="warn" />
          </div>
        }
        <div
          class="right-2 bottom-2 absolute bg-black bg-opacity-75 px-2 py-1 rounded text-white text-xs"
        >
          {{ formatDuration(video().durationSeconds) }}
        </div>
      </div>

      <!-- Content -->
      <div class="p-4 flex flex-col flex-1">
        <h3 class="mb-2 font-semibold text-gray-900 text-lg line-clamp-2">
          {{ video().title }}
        </h3>

        @if (video().description) {
          <p class="mb-3 text-gray-600 text-sm line-clamp-2">
            {{ video().description }}
          </p>
        }

        <!-- Actions -->
        <div class="flex gap-2 mt-auto">
          <p-button
            label="Edit"
            icon="pi pi-pencil"
            [outlined]="true"
            size="small"
            class="flex-1"
            (onClick)="edit.emit()"
          />
          <p-button
            icon="pi pi-trash"
            [outlined]="true"
            severity="danger"
            size="small"
            (onClick)="delete.emit()"
          />
        </div>
      </div>
    </div>
  `,
})
export class VideoCardComponent {
  video = input.required<Video>();
  edit = output<void>();
  delete = output<void>();

  formatDuration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  }
}

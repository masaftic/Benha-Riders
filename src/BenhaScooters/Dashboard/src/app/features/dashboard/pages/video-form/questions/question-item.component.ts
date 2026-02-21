import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { Question } from '../../../../../core/models/question.model';

@Component({
  selector: 'app-question-item',
  imports: [ButtonModule],
  template: `
    <div class="border border-gray-200 rounded-lg p-3 bg-white hover:shadow-sm transition-shadow my-2">
      <div class="flex items-start gap-3">
        <!-- Timestamp Badge -->
        <div class="flex-shrink-0">
          <span class="inline-flex items-center justify-center bg-blue-500 text-white text-xs font-medium px-2.5 py-1 rounded">
            {{ formatTimestamp(question().timestampSeconds) }}
          </span>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium text-gray-900 mb-1.5 line-clamp-2">
            {{ question().text }}
          </p>
          <div class="flex items-center gap-3 text-xs text-gray-500">
            <span class="flex items-center gap-1 text-nowrap">
              <i class="pi pi-list text-xs"></i>
              {{ question().options.length }} options
            </span>
            <span class="flex items-center gap-1 text-nowrap">
              <i class="pi pi-star-fill text-xs"></i>
              {{ question().pointsWorth }} pts
            </span>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex gap-1 flex-shrink-0">
          <p-button
            icon="pi pi-pencil"
            [text]="true"
            [rounded]="true"
            size="small"
            severity="secondary"
            (onClick)="edit.emit()"
          />
          <p-button
            icon="pi pi-trash"
            [text]="true"
            [rounded]="true"
            size="small"
            severity="danger"
            (onClick)="delete.emit()"
          />
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class QuestionItemComponent {
  question = input.required<Question>();
  edit = output<void>();
  delete = output<void>();

  formatTimestamp(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }
}

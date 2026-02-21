import { Component, input, signal, effect, inject, ChangeDetectionStrategy } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { QuestionService } from '../../../../../core/services/question.service';
import { CreateQuestionDto, UpdateQuestionDto, Question } from '../../../../../core/models/question.model';
import { QuestionItemComponent } from './question-item.component';
import { QuestionFormComponent } from './question-form.component';

@Component({
  selector: 'app-question-list',
  imports: [
    ButtonModule,
    Dialog,
    ToastModule,
    ConfirmDialogModule,
    QuestionItemComponent,
    QuestionFormComponent,
  ],
  providers: [MessageService, ConfirmationService],
  template: `
    <div class="flex flex-col h-full">
      <!-- Header -->
      <div class="flex justify-between items-center mb-4">
        <h3 class="font-semibold text-gray-900 text-lg">
          Questions
          @if (questions().length > 0) {
            <span class="text-sm text-gray-500 font-normal">({{ questions().length }})</span>
          }
        </h3>
        <p-button
          label="Add"
          icon="pi pi-plus"
          size="small"
          (onClick)="openCreateDialog()"
        />
      </div>

      <!-- Questions List -->
      @if (questions().length > 0) {
        <div class="space-y-2 flex-1 overflow-y-auto">
          @for (question of questions(); track question.id) {
            <app-question-item
              [question]="question"
              (edit)="openEditDialog(question)"
              (delete)="confirmDelete(question)"
            />
          }
        </div>
      } @else {
        <div class="flex flex-col justify-center items-center py-8 text-center flex-1">
          <i class="mb-3 text-gray-400 text-4xl pi pi-question-circle"></i>
          <p class="text-gray-600 mb-1">No questions yet</p>
          <p class="text-gray-500 text-sm mb-4">Add questions to engage viewers</p>
          <p-button
            label="Add First Question"
            icon="pi pi-plus"
            size="small"
            (onClick)="openCreateDialog()"
          />
        </div>
      }
    </div>

    <!-- Question Form Dialog -->
    <p-dialog
      [(visible)]="showDialog"
      [header]="editingQuestion() ? 'Edit Question' : 'Add Question'"
      [modal]="true"
      [style]="{ width: '50vw' }"
      [breakpoints]="{ '960px': '75vw', '640px': '90vw' }"
    >
      @if (showDialog()) {
        <app-question-form
          [videoId]="videoId()"
          [existingQuestion]="editingQuestion()"
          (save)="handleSave($event)"
          (cancel)="closeDialog()"
        />
      }
    </p-dialog>

    <p-toast />
    <p-confirmDialog />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class QuestionListComponent {
  private readonly questionService = inject(QuestionService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  videoId = input.required<string>();
  questions = signal<Question[]>([]);
  showDialog = signal(false);
  editingQuestion = signal<Question | null>(null);

  constructor() {
    // Load questions when videoId changes
    effect(() => {
      const id = this.videoId();
      if (id) {
        this.loadQuestions();
      }
    });
  }

  loadQuestions(): void {
    this.questionService.getQuestionsByVideoId(this.videoId()).subscribe({
      next: (questions) => {
        this.questions.set(questions);
      }
    });
  }

  openCreateDialog(): void {
    this.editingQuestion.set(null);
    this.showDialog.set(true);
  }

  openEditDialog(question: Question): void {
    this.editingQuestion.set(question);
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.editingQuestion.set(null);
  }

  handleSave(event: CreateQuestionDto | { id: number; data: UpdateQuestionDto }): void {
    if ('id' in event) {
      // Update existing question
      this.questionService.updateQuestion(event.id, event.data).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Question updated successfully'
          });
          this.loadQuestions();
          this.closeDialog(); // Only close on success
        }
      });
    } else {
      // Create new question
      this.questionService.createQuestion(event).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Question created successfully'
          });
          this.loadQuestions();
          this.closeDialog(); // Only close on success
        }
      });
    }
  }

  confirmDelete(question: Question): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete this question?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deleteQuestion(question.id)
    });
  }

  deleteQuestion(id: number): void {
    this.questionService.deleteQuestion(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Question deleted successfully'
        });
        this.loadQuestions();
      }
    });
  }
}

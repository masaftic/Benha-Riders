import { Component, input, output, signal, effect, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { RadioButton } from 'primeng/radiobutton';
import { CreateQuestionDto, UpdateQuestionDto, Question } from '../../../../../core/models/question.model';

@Component({
  selector: 'app-question-form',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumber,
    Textarea,
    RadioButton,
  ],
  template: `
    <form [formGroup]="questionForm" (ngSubmit)="submit()" class="space-y-4">
      <!-- Timestamp -->
      <div class="flex flex-col gap-2">
        <label for="timestamp" class="font-semibold text-sm">
          Timestamp (seconds) <span class="text-red-500">*</span>
        </label>
        <p-inputnumber
          inputId="timestamp"
          formControlName="timestampSeconds"
          [min]="0"
          [showButtons]="true"
          placeholder="Enter timestamp in seconds"
          class="w-full"
        />
        @if (questionForm.get('timestampSeconds')?.invalid && questionForm.get('timestampSeconds')?.touched) {
          <small class="text-red-500">Timestamp is required and must be 0 or greater</small>
        }
      </div>

      <!-- Question Text -->
      <div class="flex flex-col gap-2">
        <label for="text" class="font-semibold text-sm">
          Question Text <span class="text-red-500">*</span>
        </label>
        <textarea
          pTextarea
          id="text"
          formControlName="text"
          placeholder="Enter your question"
          rows="3"
          class="w-full"
        ></textarea>
        @if (questionForm.get('text')?.invalid && questionForm.get('text')?.touched) {
          <small class="text-red-500">Question must be at least 3 characters</small>
        }
      </div>

      <!-- Options -->
      <div class="flex flex-col gap-2">
        <div class="flex justify-between items-center">
          <label class="font-semibold text-sm">
            Options <span class="text-red-500">*</span> (min 2)
          </label>
          <p-button
            label="Add Option"
            icon="pi pi-plus"
            size="small"
            [text]="true"
            type="button"
            (onClick)="addOption()"
          />
        </div>

        <div formArrayName="options" class="space-y-2">
          @for (option of options.controls; track option; let i = $index) {
            <div class="flex gap-2 items-start">
              <div class="flex items-center pt-2">
                <p-radiobutton
                  [value]="i"
                  [formControl]="correctAnswerIndexControl"
                  [inputId]="'correct-' + i"
                />
              </div>
              <input
                pInputText
                [formControlName]="i"
                [placeholder]="'Option ' + (i + 1)"
                class="flex-1"
              />
              @if (options.controls.length > 2) {
                <p-button
                  icon="pi pi-trash"
                  severity="danger"
                  [text]="true"
                  size="small"
                  type="button"
                  (onClick)="removeOption(i)"
                />
              }
            </div>
          }
        </div>
        @if (options.invalid && options.touched) {
          <small class="text-red-500">At least 2 options are required</small>
        }
      </div>

      <!-- Points Worth -->
      <div class="flex flex-col gap-2">
        <label for="points" class="font-semibold text-sm">
          Points Worth <span class="text-red-500">*</span>
        </label>
        <p-inputnumber
          inputId="points"
          formControlName="pointsWorth"
          [min]="1"
          [max]="100"
          [showButtons]="true"
          placeholder="Points (1-100)"
          class="w-full"
        />
        @if (questionForm.get('pointsWorth')?.invalid && questionForm.get('pointsWorth')?.touched) {
          <small class="text-red-500">Points must be between 1 and 100</small>
        }
      </div>

      <!-- Explanation -->
      <div class="flex flex-col gap-2">
        <label for="explanation" class="font-semibold text-sm">Explanation (optional)</label>
        <textarea
          pTextarea
          id="explanation"
          formControlName="explanation"
          placeholder="Explain the correct answer"
          rows="2"
          class="w-full"
        ></textarea>
      </div>

      <!-- Actions -->
      <div class="flex justify-end gap-2 pt-4">
        <p-button
          label="Cancel"
          severity="secondary"
          type="button"
          (onClick)="cancel.emit()"
        />
        <p-button
          label="Save"
          type="submit"
          [disabled]="questionForm.invalid"
        />
      </div>
    </form>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class QuestionFormComponent {
  private readonly fb = inject(FormBuilder);

  videoId = input.required<string>();
  existingQuestion = input<Question | null>(null);

  save = output<CreateQuestionDto | { id: number; data: UpdateQuestionDto }>();
  cancel = output<void>();

  questionForm: FormGroup;

  constructor() {
    this.questionForm = this.fb.group({
      timestampSeconds: [0, [Validators.required, Validators.min(0)]],
      text: ['', [Validators.required, Validators.minLength(3)]],
      options: this.fb.array(
        ['', ''],
        [Validators.required, Validators.minLength(2)]
      ),
      correctAnswerIndex: [0, [Validators.required, Validators.min(0)]],
      explanation: [''],
      pointsWorth: [10, [Validators.required, Validators.min(1), Validators.max(100)]]
    });

    // Initialize options array
    this.addOption();

    // Load existing question if provided
    effect(() => {
      const question = this.existingQuestion();
      if (question) {
        this.loadQuestion(question);
      } else {
        this.reset();
      }
    });
  }

  reset(): void {
    this.questionForm.reset({
      timestampSeconds: 0,
      text: '',
      pointsWorth: 10,
      correctAnswerIndex: 0,
      explanation: ''
    });
    // Reset options to two empty
    while (this.options.length > 0) {
      this.options.removeAt(0);
    }
    this.addOption();
    this.addOption();
  }

  get options(): FormArray {
    return this.questionForm.get('options') as FormArray;
  }

  get correctAnswerIndexControl() {
    return this.questionForm.get('correctAnswerIndex') as any;
  }

  addOption(): void {
    this.options.push(this.fb.control('', Validators.required));
  }

  removeOption(index: number): void {
    if (this.options.length > 2) {
      this.options.removeAt(index);

      // Adjust correctAnswerIndex if needed
      const currentCorrect = this.questionForm.get('correctAnswerIndex')?.value;
      if (currentCorrect === index) {
        this.questionForm.patchValue({ correctAnswerIndex: 0 });
      } else if (currentCorrect > index) {
        this.questionForm.patchValue({ correctAnswerIndex: currentCorrect - 1 });
      }
    }
  }

  loadQuestion(question: Question): void {
    // Clear existing options
    while (this.options.length > 0) {
      this.options.removeAt(0);
    }

    // Add options from question
    question.options.forEach(option => {
      this.options.push(this.fb.control(option, Validators.required));
    });

    // Patch form values
    this.questionForm.patchValue({
      timestampSeconds: question.timestampSeconds,
      text: question.text,
      pointsWorth: question.pointsWorth,
      correctAnswerIndex: question.correctAnswerIndex,
      explanation: question.explanation
    });
  }

  submit(): void {
    if (this.questionForm.invalid) {
      this.questionForm.markAllAsTouched();
      return;
    }

    const formValue = this.questionForm.value;
    const existing = this.existingQuestion();

    if (existing) {
      // Update existing question
      const updateDto: UpdateQuestionDto = {
        timestampSeconds: formValue.timestampSeconds,
        text: formValue.text,
        options: formValue.options,
        correctAnswerIndex: formValue.correctAnswerIndex,
        explanation: formValue.explanation || null,
        pointsWorth: formValue.pointsWorth
      };
      this.save.emit({ id: existing.id, data: updateDto });
    } else {
      // Create new question
      const createDto: CreateQuestionDto = {
        videoId: this.videoId(),
        timestampSeconds: formValue.timestampSeconds,
        text: formValue.text,
        options: formValue.options,
        correctAnswerIndex: formValue.correctAnswerIndex,
        explanation: formValue.explanation || null,
        pointsWorth: formValue.pointsWorth
      };
      this.save.emit(createDto);
    }
  }
}

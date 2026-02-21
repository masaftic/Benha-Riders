import { Component, inject, signal, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumber, InputNumberClasses, InputNumberModule } from 'primeng/inputnumber';
import { Textarea } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { VideoService } from '../../../../core/services/video.service';
import { TagService } from '../../../../core/services/tag.service';
import { Tag } from '../../../../core/models/tag.model';
import { Video } from '../../../../core/models/video.model';
import { QuestionListComponent } from './questions/question-list.component';

@Component({
  selector: 'app-video-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    InputNumber,
    Textarea,
    Select,
    ToggleSwitch,
    ToastModule,
    ConfirmDialogModule,
    QuestionListComponent,
    InputNumberModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './video-form.component.html',
  styleUrl: './video-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class VideoFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly videoService = inject(VideoService);
  private readonly tagService = inject(TagService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  isEditMode = signal(false);
  videoId = signal<string | null>(null);
  tags = signal<Tag[]>([]);
  isSubmitting = signal(false);

  videoFile = signal<File | null>(null);
  videoPreviewUrl = signal<string | null>(null);

  thumbnailFile = signal<File | null>(null);
  thumbnailPreviewUrl = signal<string | null>(null);

  videoForm: FormGroup = this.fb.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    durationSeconds: [0, [Validators.required, Validators.min(1)]],
    tagId: [null, Validators.required],
    isVisibleToGuests: [false],
    canGuestsPlay: [false]
  });

  goBack() {
    this.router.navigate(['/dashboard/videos']);
  }

  ngOnInit(): void {
    this.loadTags();

    const id = this.route.snapshot.paramMap.get('id');
    console.log('VideoFormComponent initialized with id:', id);
    if (id) {
      this.isEditMode.set(true);
      this.videoId.set(id);
      this.loadVideo(id);
    }

    // Handle conditional enabling of canGuestsPlay
    this.videoForm.get('isVisibleToGuests')?.valueChanges.subscribe((isVisible) => {
      const canGuestsPlayControl = this.videoForm.get('canGuestsPlay');
      if (isVisible) {
        canGuestsPlayControl?.enable();
      } else {
        canGuestsPlayControl?.setValue(false);
        canGuestsPlayControl?.disable();
      }
    });

    // Initialize the state
    if (!this.videoForm.get('isVisibleToGuests')?.value) {
      this.videoForm.get('canGuestsPlay')?.disable();
    }
  }

  loadTags(): void {
    this.tagService.getTags().subscribe({
      next: (tags) => {
        this.tags.set(tags);
      }
    });
  }

  loadVideo(id: string): void {
    this.videoService.getVideoById(id).subscribe({
      next: (video) => {
        this.videoForm.patchValue({
          title: video.title,
          description: video.description,
          durationSeconds: video.durationSeconds,
          tagId: video.tagId,
          isVisibleToGuests: video.isVisibleToGuests,
          canGuestsPlay: video.canGuestsPlay
        });

        // Set preview URLs from existing video
        this.videoPreviewUrl.set(video.videoUrl);
        this.thumbnailPreviewUrl.set(video.thumbnailUrl);
      },
      error: () => {
        this.router.navigate(['/dashboard/videos']);
      }
    });
  }

  onVideoSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.videoFile.set(file);

      // Create preview URL
      const url = URL.createObjectURL(file);
      this.videoPreviewUrl.set(url);
    }
  }

  onThumbnailSelect(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      const file = input.files[0];
      this.thumbnailFile.set(file);

      // Create preview URL
      const url = URL.createObjectURL(file);
      this.thumbnailPreviewUrl.set(url);
    }
  }

  removeVideo(): void {
    this.videoFile.set(null);
    if (this.videoPreviewUrl() && this.videoPreviewUrl()!.startsWith('blob:')) {
      URL.revokeObjectURL(this.videoPreviewUrl()!);
    }
    this.videoPreviewUrl.set(null);
  }

  removeThumbnail(): void {
    this.thumbnailFile.set(null);
    if (this.thumbnailPreviewUrl() && this.thumbnailPreviewUrl()!.startsWith('blob:')) {
      URL.revokeObjectURL(this.thumbnailPreviewUrl()!);
    }
    this.thumbnailPreviewUrl.set(null);
  }

  save(): void {
    if (this.videoForm.invalid) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fill in all required fields correctly'
      });
      return;
    }

    const formValue = this.videoForm.value;

    console.log('Submitting video form with values:', formValue);
    this.isSubmitting.set(true);

    if (this.isEditMode()) {
      // Update existing video
      this.videoService.updateVideo(this.videoId()!, {
        title: formValue.title,
        description: formValue.description || null,
        durationSeconds: formValue.durationSeconds,
        videoFile: this.videoFile() || undefined,
        thumbnailFile: this.thumbnailFile() || undefined,
        tagId: formValue.tagId,
        isVisibleToGuests: formValue.isVisibleToGuests,
        canGuestsPlay: formValue.canGuestsPlay || false
      }).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.videoForm.markAsPristine();
          this.videoFile.set(null);
          this.thumbnailFile.set(null);
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Video updated successfully'
          });
          this.router.navigate(['/dashboard/videos/edit/', this.videoId()!]);
        },
        error: () => {
          this.isSubmitting.set(false);
        }
      });
    } else {
      // Create new video
      if (!this.videoFile() || !this.thumbnailFile()) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Missing Files',
          detail: 'Please upload both video and thumbnail files'
        });
        this.isSubmitting.set(false);
        return;
      }

      this.videoService.createVideo({
        title: formValue.title,
        description: formValue.description || null,
        durationSeconds: formValue.durationSeconds,
        videoFile: this.videoFile()!,
        thumbnailFile: this.thumbnailFile()!,
        tagId: formValue.tagId,
        isVisibleToGuests: formValue.isVisibleToGuests,
        canGuestsPlay: formValue.canGuestsPlay || false
      }).subscribe({
        next: (response) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Video created successfully'
          });
          this.isSubmitting.set(false);
          setTimeout(() => {
            this.router.navigate(['/dashboard/videos/edit/', response.id]);
          }, 2000);
        },
        error: () => {
          this.isSubmitting.set(false);
        }
      });
    }
  }

  cancel(): void {
    if (this.videoForm.dirty || this.videoFile() || this.thumbnailFile()) {
      this.confirmationService.confirm({
        message: 'You have unsaved changes. Are you sure you want to cancel?',
        header: 'Confirm Cancel',
        icon: 'pi pi-exclamation-triangle',
        accept: () => this.router.navigate(['/dashboard/videos'])
      });
    } else {
      this.router.navigate(['/dashboard/videos']);
    }
  }
}

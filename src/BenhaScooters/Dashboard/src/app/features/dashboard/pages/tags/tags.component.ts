import { Component, inject, signal, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TagService } from '../../../../core/services/tag.service';
import { Tag } from '../../../../core/models/tag.model';

@Component({
  selector: 'app-tags',
  imports: [
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    ReactiveFormsModule,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './tags.component.html',
  styleUrl: './tags.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TagsComponent implements OnInit {
  private readonly tagService = inject(TagService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  tags = signal<Tag[]>([]);
  filteredTags = signal<Tag[]>([]);
  showDialog = signal(false);
  isEditMode = signal(false);
  editingTagId = signal<number | null>(null);

  tagForm: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(50)]]
  });

  searchValue = '';

  ngOnInit(): void {
    this.loadTags();
  }

  loadTags(): void {
    this.tagService.getTags().subscribe({
      next: (tags) => {
        this.tags.set(tags);
        this.filteredTags.set(tags);
      }
    });
  }

  onGlobalFilter(event: Event): void {
    const value = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchValue = value;

    if (!value) {
      this.filteredTags.set(this.tags());
      return;
    }

    const filtered = this.tags().filter(tag =>
      tag.name.toLowerCase().includes(value) ||
      tag.id.toString().includes(value)
    );
    this.filteredTags.set(filtered);
  }

  openCreateDialog(): void {
    this.isEditMode.set(false);
    this.editingTagId.set(null);
    this.tagForm.reset();
    this.showDialog.set(true);
  }

  openEditDialog(tag: Tag): void {
    this.isEditMode.set(true);
    this.editingTagId.set(tag.id);
    this.tagForm.patchValue({ name: tag.name });
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.tagForm.reset();
  }

  saveTag(): void {
    if (this.tagForm.invalid) {
      return;
    }

    const formValue = this.tagForm.value;

    if (this.isEditMode()) {
      this.tagService.updateTag(this.editingTagId()!, { name: formValue.name }).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Tag updated successfully'
          });
          this.loadTags();
          this.closeDialog();
        }
      });
    } else {
      this.tagService.createTag({ name: formValue.name }).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Tag created successfully'
          });
          this.loadTags();
          this.closeDialog();
        }
      });
    }
  }

  confirmDelete(tag: Tag): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${tag.name}"?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.deleteTag(tag.id)
    });
  }

  deleteTag(id: number): void {
    this.tagService.deleteTag(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Tag deleted successfully'
        });
        this.loadTags();
      }
    });
  }
}

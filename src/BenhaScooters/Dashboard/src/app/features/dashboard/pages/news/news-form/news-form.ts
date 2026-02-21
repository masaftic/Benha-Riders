import { Component, computed, inject, input, output } from '@angular/core';
import { CreateNewsDto, NewsItem, UpdateNewsDto } from '../../../../../core/models/news.model';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { Button } from "primeng/button";

@Component({
  selector: 'app-news-form',
  imports: [ReactiveFormsModule, InputTextModule, Button],
  template: `
    <div class="">
      <form [formGroup]="form" (ngSubmit)="submit()">
        <div class="mb-4">
          <label for="title" class="block text-gray-700 font-semibold mb-2">Title</label>
          <input pInputText id="title" type="text" formControlName="title" fluid />
          @if (form.get('title')?.invalid && form.get('title')?.touched) {
            <div class="text-red-500 text-sm mt-1">
              Title must be at least 3 characters long.
            </div>
          }
        </div>

        <div class="mb-4">
          <label for="subtitle" class="block text-gray-700 font-semibold mb-2">Subtitle</label>
          <textarea pInputText id="subtitle" type="text" formControlName="subtitle" fluid rows="3"></textarea>
          @if (form.get('subtitle')?.invalid && form.get('subtitle')?.touched) {
            <div class="text-red-500 text-sm mt-1">
              Subtitle must be at least 3 characters long.
            </div>
          }
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 font-semibold mb-2" for="file_input">Upload file</label>
          <input class="block w-full text-sm text-gray-500 cursor-pointer
              file:mr-4 file:py-2 file:px-4
              file:border-0
              file:cursor-pointer
              transition-colors
              duration-150
              border border-dashed border-gray-300
              file:text-sm file:font-semibold
              file:bg-[#ea580c] file:text-white
              hover:file:bg-[#c2410c]" id="file_input" type="file" (change)="onFileChange($event)" (cancel)="$event.stopPropagation()" />
          @if (form.get('image')?.invalid && form.get('image')?.touched) {
            <div class="text-red-500 text-sm mt-1">
              Image is required.
            </div>
          }
        </div>

        @if (imagePreview) {
          <div class="mb-4">
            <label class="block text-gray-700 font-semibold mb-2">Image Preview</label>
            <img [src]="imagePreview" alt="Image Preview" class="bg-gray-100 border rounded-lg w-full max-h-96 object-contain" />
          </div>
        }

        <div class="flex justify-end space-x-2 mt-8">
          <p-button type="submit" class="font-bold" [disabled]="form.invalid">
            {{ isEditMode() ? 'Update News' : 'Create News' }}
          </p-button>
          <p-button type="button" (click)="cancel.emit()" severity="secondary" class="font-semibold">
            Cancel
          </p-button>
        </div>
      </form>
    </div>
  `,
  styles: [``],
})
export class NewsForm {
  newsItem = input<NewsItem | null>(null);
  isEditMode = computed(() => this.newsItem() !== null);

  save = output<UpdateNewsDto | CreateNewsDto>();
  cancel = output<void>();

  fb = inject(FormBuilder);

  form = this.fb.group({
    title: ['', [Validators.required, Validators.minLength(3)]],
    subtitle: ['', [Validators.required, Validators.minLength(3)]],
    image: [null as File | null],
  });

  ngOnInit() {
    console.log('newsItem', this.newsItem());
    console.log('isEditMode', this.isEditMode());


    const imageControl = this.form.get('image');
    if (this.isEditMode()) {
      imageControl?.clearValidators();
    } else {
      imageControl?.setValidators([Validators.required]);
    }

    imageControl?.updateValueAndValidity();

    this.form.valueChanges.subscribe(() => {
      console.log(this.form.value);
      console.log(this.form.valid);
    });
    if (this.newsItem()) {
      this.form.patchValue({
        title: this.newsItem()!.title,
        subtitle: this.newsItem()!.subtitle,
      });
    }
  }

  onFileChange(event: any) {
    console.log(event);
    const file = event.target.files[0];
    if (file) {
      this.form.patchValue({ image: file });
    }
  }

  // image blob for preview
  get imagePreview() {
    const imageFile = this.form.get('image')?.value;
    if (imageFile) {
      return URL.createObjectURL(imageFile);
    } else if (this.newsItem()) {
      return this.newsItem()!.imageUrl;
    }
    return null;
  }


  submit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (!this.isEditMode()) {
      // do not allow empty image on create
      if (!this.form.get('image')?.value) {
        this.form.get('image')?.setErrors({ required: true });
        this.form.markAllAsTouched();
        return;
      }
    }

    const formValue = this.form.value;
    // Handle form submission logic here
    if (this.isEditMode()) {
      const updatedNewsItem: UpdateNewsDto = {
        title: formValue.title!,
        subtitle: formValue.subtitle!,
        image: formValue.image ? formValue.image : undefined,
      };

      this.save.emit(updatedNewsItem);
    } else {
      const newNewsItem: CreateNewsDto = {
        title: formValue.title!,
        subtitle: formValue.subtitle!,
        image: formValue.image!,
      };

      this.save.emit(newNewsItem);
    }
  }
}

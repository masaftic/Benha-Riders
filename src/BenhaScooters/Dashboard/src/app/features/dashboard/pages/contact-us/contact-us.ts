import { Component, inject, signal, ChangeDetectionStrategy, OnInit, computed } from '@angular/core';
import { ContactUsService } from '../../../../core/services/contact-us.service';
import { ContactUsResponse } from '../../../../core/models/contact-us.model';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { DatePipe } from '@angular/common';
import { debounceSignal } from '../../../../core/utils';
import { toObservable } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';

@Component({
  selector: 'app-contact-us',
  imports: [
    TableModule,
    ButtonModule,
    InputTextModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
    DialogModule,
    DatePipe
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './contact-us.html',
  styleUrl: './contact-us.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ContactUs implements OnInit {
  private readonly contactUsService = inject(ContactUsService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

  contacts = signal<ContactUsResponse[]>([]);
  totalRecords = signal<number>(0);
  loading = signal<boolean>(false);
  showDialog = signal(false);
  selectedContact = signal<ContactUsResponse | null>(null);

  searchInputVal = signal<string>('');
  searchQuery = debounceSignal(this.searchInputVal, 500, '');
  pageNumber = signal<number>(1);
  pageSize = signal<number>(10);
  refreshTrigger = signal<number>(0);

  query = computed(() => ({
    pageNumber: this.pageNumber(),
    pageSize: this.pageSize(),
    search: this.searchQuery(),
    orderBy: 'createdAt',
    isDescending: true,
    reload: this.refreshTrigger()
  }));

  constructor() {
    toObservable(this.query).pipe(
      tap(() => this.loading.set(true)),
      switchMap(query =>
        this.contactUsService.getAll(query).pipe(
          catchError(() => {
            this.loading.set(false);
            return of(null);
          })
        )
      )
    ).subscribe(response => {
      if (!response) return;

      this.contacts.set(response.items);
      this.totalRecords.set(response.totalCount);
      this.loading.set(false);
    });
  }

  ngOnInit(): void {}

  onGlobalFilter(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchInputVal.set(value);
    this.pageNumber.set(1);
  }

  onPageChange(event: TableLazyLoadEvent): void {
    this.pageNumber.set((event.first! / event.rows!) + 1);
    this.pageSize.set(event.rows!);
  }

  viewContact(contact: ContactUsResponse): void {
    this.selectedContact.set(contact);
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.selectedContact.set(null);
  }

  confirmDelete(contact: ContactUsResponse): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete the message from ${contact.fullName}?`,
      header: 'Delete Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.deleteContact(contact.id);
      }
    });
  }

  deleteContact(id: number): void {
    this.contactUsService.delete(id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Contact record deleted successfully'
        });
        // Trigger reload by updating a signal in the query
        this.refreshTrigger.update(n => n + 1);
      }
    });
  }
}

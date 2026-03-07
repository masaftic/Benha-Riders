import { TableLazyLoadEvent } from "primeng/table";
import { DriverSummary, OnboardingStatus } from "../../../../core/models/driver.model";
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals'
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { computed, inject } from "@angular/core";
import { finalize, pipe, switchMap, tap } from "rxjs";
import { DriverService } from "../../../../core/services/driver.service";
import { tapResponse } from "@ngrx/operators";


type DriversPageState = {
  drivers: Array<DriverSummary>;
  totalRecords: number;
  loading: boolean;
  selectedStatus: OnboardingStatus;
  pageNumber: number;
  pageSize: number;
};


const initialState: DriversPageState = {
  drivers: [],
  loading: false,
  pageNumber: 1,
  pageSize: 10,
  selectedStatus: "Incomplete",
  totalRecords: 0
}


export const DriversPageStore = signalStore(
  { providedIn: 'root' },
  withState<DriversPageState>(initialState),
  withComputed((state) => ({
    query: computed(() => ({
      onboardingStatus: state.selectedStatus(),
      pageNumber: state.pageNumber(),
      pageSize: state.pageSize()
    })),
    severityStatus: computed(() => {
      const severityMap: Record<OnboardingStatus, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
        'Incomplete': 'warn',
        'UnderReview': 'info',
        'Approved': 'success',
        'Rejected': 'danger',
        'Suspended': 'secondary'
      };
      return severityMap[state.selectedStatus()];
    })
  })),
  withMethods((store, driverService = inject(DriverService)) => ({
    updateStatus(status: OnboardingStatus) {
      patchState(store, { selectedStatus: status, pageNumber: 1 })
    },
    updatePage(event: TableLazyLoadEvent) {
      patchState(store, { pageNumber: (event.first! / event.rows!) + 1, pageSize: event.rows! })
    },
    loadByQuery: rxMethod<{ onboardingStatus: OnboardingStatus; pageNumber: number; pageSize: number; }>(
      pipe(
        tap(() => patchState(store, { loading: true })),
        switchMap((query) => {
          return driverService.getAll(query).pipe(
            tapResponse({
              next: (response) => {
                patchState(store, {
                  drivers: response.items,
                  totalRecords: response.totalCount,
                })
              },
              error: (error) => {
                console.error('Failed to load drivers', error);
              },
              finalize: () => patchState(store, { loading: false })
            })
          )
        })
      ))
  })),
);

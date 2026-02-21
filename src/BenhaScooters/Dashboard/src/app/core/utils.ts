import { Signal } from "@angular/core";
import { toObservable, toSignal } from "@angular/core/rxjs-interop";
import { debounceTime } from "rxjs";


export function debounceSignal<T>(signal: Signal<T>, delay: number, initialValue: T): Signal<T> {
  const signalObs = toObservable(signal);

  return toSignal(
    signalObs.pipe(
      debounceTime(delay)
    ),
    { initialValue }
  )
}

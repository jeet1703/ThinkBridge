import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Quote, QuotesApiService } from './quotes-api.service';

@Component({
  selector: 'app-quotes-explorer',
  standalone: true,
  templateUrl: './quotes-explorer.component.html',
  styleUrl: './quotes-explorer.component.css'
})
export class QuotesExplorerComponent {
  // inject() for the service — no constructor injection anywhere in this component.
  private readonly quotesApi = inject(QuotesApiService);

  // --- list state ---
  protected readonly quotes = signal<Quote[]>([]);
  protected readonly listLoading = signal(true);
  protected readonly listError = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(5);
  protected readonly total = signal(0);

  // --- detail state ---
  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedQuote = signal<Quote | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly detailError = signal<string | null>(null);

  /** Monotonic counters used purely to detect + discard stale responses (not rendered, so plain fields, not signals). */
  private listRequestSeq = 0;
  private detailRequestSeq = 0;

  /** Derived from two signals: total + pageSize. */
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  protected readonly listViewState = computed<'loading' | 'error' | 'empty' | 'success'>(() => {
    if (this.listLoading()) return 'loading';
    if (this.listError()) return 'error';
    if (this.quotes().length === 0) return 'empty';
    return 'success';
  });

  protected readonly detailViewState = computed<'idle' | 'loading' | 'error' | 'success'>(() => {
    if (this.selectedId() === null) return 'idle';
    if (this.detailLoading()) return 'loading';
    if (this.detailError()) return 'error';
    return 'success';
  });

  constructor() {
    // Re-fetch whenever the requested page (or page size) changes — starts at
    // page=1&size=5, per the exercise, but is no longer pinned to just that call.
    effect(() => {
      const page = this.page();
      const size = this.pageSize();
      this.loadList(page, size);
    });

    // The detail fetch DOES need to react to selectedId changing (a new quote clicked).
    effect(() => {
      const id = this.selectedId();
      if (id !== null) {
        this.loadDetail(id);
      }
    });
  }

  protected retryList(): void {
    this.loadList(this.page(), this.pageSize());
  }

  protected retryDetail(): void {
    const id = this.selectedId();
    if (id !== null) {
      this.loadDetail(id);
    }
  }

  protected nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.closeDetail();
      this.page.update((p) => p + 1);
    }
  }

  protected previousPage(): void {
    if (this.page() > 1) {
      this.closeDetail();
      this.page.update((p) => p - 1);
    }
  }

  protected select(id: number): void {
    // Reset detail state synchronously — loadDetail() (triggered by the effect below)
    // only runs on the next microtask, so without this, the OLD quote's detail stays on
    // screen — now mislabeled under the newly-selected id — until the fetch catches up.
    this.selectedQuote.set(null);
    this.detailError.set(null);
    this.detailLoading.set(true);
    this.selectedId.set(id);
  }

  protected closeDetail(): void {
    this.selectedId.set(null);
    this.selectedQuote.set(null);
    this.detailError.set(null);
  }

  private loadList(page: number, size: number): void {
    // Tag this dispatch; if a newer one starts before this resolves, its response is discarded on arrival.
    const requestId = ++this.listRequestSeq;

    this.listLoading.set(true);
    this.listError.set(null);

    this.quotesApi.getQuotes(page, size).subscribe({
      next: (response) => {
        if (requestId !== this.listRequestSeq) return; // a newer list request superseded this one
        this.quotes.set(response.items);
        this.total.set(response.total);
        this.listLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        if (requestId !== this.listRequestSeq) return;
        this.listError.set(
          err.status === 0
            ? 'Could not reach the Quotes API. Is it running?'
            : `Failed to load quotes (HTTP ${err.status}).`
        );
        this.listLoading.set(false);
      }
    });
  }

  private loadDetail(id: number): void {
    // Same guard on the detail fetch: clicking a second quote before the first
    // one's response arrives must not let the first (now stale) response win.
    const requestId = ++this.detailRequestSeq;

    this.detailLoading.set(true);
    this.detailError.set(null);

    this.quotesApi.getQuoteById(id).subscribe({
      next: (response) => {
        if (requestId !== this.detailRequestSeq) return; // a newer detail request superseded this one
        this.selectedQuote.set(response);
        this.detailLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        if (requestId !== this.detailRequestSeq) return;
        this.detailError.set(
          err.status === 404
            ? `Quote ${id} was not found.`
            : err.status === 0
              ? 'Could not reach the Quotes API. Is it running?'
              : `Failed to load quote (HTTP ${err.status}).`
        );
        this.detailLoading.set(false);
      }
    });
  }
}

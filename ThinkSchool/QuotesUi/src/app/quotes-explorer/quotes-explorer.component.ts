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

  // --- detail state ---
  protected readonly selectedId = signal<number | null>(null);
  protected readonly selectedQuote = signal<Quote | null>(null);
  protected readonly detailLoading = signal(false);
  protected readonly detailError = signal<string | null>(null);

  /** Monotonic counters used purely to detect + discard stale responses (not rendered, so plain fields, not signals). */
  private listRequestSeq = 0;
  private detailRequestSeq = 0;

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
    // Fixed page=1&size=5 per the exercise — a one-off fetch, no reactive dependency to key an effect() off.
    this.loadList();

    // The detail fetch DOES need to react to selectedId changing (a new quote clicked).
    effect(() => {
      const id = this.selectedId();
      if (id !== null) {
        this.loadDetail(id);
      }
    });
  }

  protected retryList(): void {
    this.loadList();
  }

  protected retryDetail(): void {
    const id = this.selectedId();
    if (id !== null) {
      this.loadDetail(id);
    }
  }

  protected select(id: number): void {
    this.selectedId.set(id);
  }

  protected closeDetail(): void {
    this.selectedId.set(null);
    this.selectedQuote.set(null);
    this.detailError.set(null);
  }

  private loadList(): void {
    // Tag this dispatch; if a newer one starts before this resolves, its response is discarded on arrival.
    const requestId = ++this.listRequestSeq;

    this.listLoading.set(true);
    this.listError.set(null);

    this.quotesApi.getQuotes(1, 5).subscribe({
      next: (response) => {
        if (requestId !== this.listRequestSeq) return; // a newer list request superseded this one
        this.quotes.set(response.items);
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

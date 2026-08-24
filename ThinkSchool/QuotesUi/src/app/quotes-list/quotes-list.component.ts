import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { QuoteDetailComponent } from '../quote-detail/quote-detail.component';

/** Shape of a single quote, matching QuotesApi's Quote model. */
export interface Quote {
  id: number;
  author: string;
  text: string;
  createdByUserId: number | null;
}

/** Shape of the GET /api/quotes response envelope. */
export interface QuotesResponse {
  page: number;
  size: number;
  total: number;
  items: Quote[];
}

@Component({
  selector: 'app-quotes-list',
  standalone: true,
  imports: [QuoteDetailComponent],
  templateUrl: './quotes-list.component.html',
  styleUrl: './quotes-list.component.css'
})
export class QuotesListComponent {
  private readonly http = inject(HttpClient);

  protected readonly quotes = signal<Quote[]>([]);
  /** Which quote's detail panel is open, if any — set by clicking a quote card. */
  protected readonly selectedQuoteId = signal<number | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly total = signal(0);
  /** Committed search term (debounced from the input) — a real `search` query param on the real API. */
  protected readonly searchTerm = signal('');

  private searchDebounceId: ReturnType<typeof setTimeout> | undefined;

  /** Derived from two signals: total + pageSize. */
  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize()))
  );

  /** Derived from three signals, drives the @switch in the template. */
  protected readonly viewState = computed<'loading' | 'error' | 'empty' | 'success'>(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    if (this.quotes().length === 0) return 'empty';
    return 'success';
  });

  /** Derived from the quotes + searchTerm signals — copy for the empty state. */
  protected readonly emptyMessage = computed(() =>
    this.searchTerm() ? `No quotes match "${this.searchTerm()}".` : 'No quotes found.'
  );

  constructor() {
    // Re-fetch whenever the requested page, page size, or search term changes.
    effect(() => {
      const page = this.page();
      const size = this.pageSize();
      const search = this.searchTerm();
      this.fetchQuotes(page, size, search);
    });
  }

  protected retry(): void {
    this.fetchQuotes(this.page(), this.pageSize(), this.searchTerm());
  }

  protected nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
    }
  }

  protected previousPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
    }
  }

  protected onSearchInput(value: string): void {
    clearTimeout(this.searchDebounceId);
    this.searchDebounceId = setTimeout(() => {
      this.page.set(1);
      this.searchTerm.set(value.trim());
    }, 300);
  }

  private fetchQuotes(page: number, size: number, search: string): void {
    this.loading.set(true);
    this.error.set(null);

    let params = new HttpParams().set('page', page).set('size', size);
    if (search) {
      params = params.set('search', search);
    }

    this.http.get<QuotesResponse>('/api/quotes', { params }).subscribe({
      next: (response) => {
        this.quotes.set(response.items);
        this.total.set(response.total);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(
          err.status === 0
            ? 'Could not reach the Quotes API. Is it running?'
            : `Failed to load quotes (HTTP ${err.status}).`
        );
        this.loading.set(false);
      }
    });
  }
}

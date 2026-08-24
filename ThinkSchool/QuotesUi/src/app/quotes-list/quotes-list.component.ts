import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';

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
  templateUrl: './quotes-list.component.html',
  styleUrl: './quotes-list.component.css'
})
export class QuotesListComponent {
  private readonly http = inject(HttpClient);

  protected readonly quotes = signal<Quote[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(10);
  protected readonly total = signal(0);

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

  constructor() {
    // Re-fetch whenever the requested page (or page size) changes.
    effect(() => {
      const page = this.page();
      const size = this.pageSize();
      this.fetchQuotes(page, size);
    });
  }

  protected retry(): void {
    this.fetchQuotes(this.page(), this.pageSize());
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

  private fetchQuotes(page: number, size: number): void {
    this.loading.set(true);
    this.error.set(null);

    const params = new HttpParams().set('page', page).set('size', size);

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

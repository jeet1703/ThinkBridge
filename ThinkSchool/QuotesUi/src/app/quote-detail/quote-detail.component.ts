import { Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Quote } from '../quotes-list/quotes-list.component';

@Component({
  selector: 'app-quote-detail',
  standalone: true,
  templateUrl: './quote-detail.component.html',
  styleUrl: './quote-detail.component.css'
})
export class QuoteDetailComponent {
  private readonly http = inject(HttpClient);

  /** Which quote to show — set by the parent whenever a quote card is clicked. */
  readonly quoteId = input.required<number>();
  readonly close = output<void>();

  protected readonly quote = signal<Quote | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly viewState = computed<'loading' | 'error' | 'success'>(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    return 'success';
  });

  constructor() {
    // Re-fetch the real GET /api/quotes/{id} endpoint whenever the selected id changes.
    effect(() => {
      this.fetchQuote(this.quoteId());
    });
  }

  protected retry(): void {
    this.fetchQuote(this.quoteId());
  }

  protected onClose(): void {
    this.close.emit();
  }

  private fetchQuote(id: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.quote.set(null);

    this.http.get<Quote>(`/api/quotes/${id}`).subscribe({
      next: (response) => {
        this.quote.set(response);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(
          err.status === 404
            ? `Quote ${id} was not found.`
            : err.status === 0
              ? 'Could not reach the Quotes API. Is it running?'
              : `Failed to load quote (HTTP ${err.status}).`
        );
        this.loading.set(false);
      }
    });
  }
}

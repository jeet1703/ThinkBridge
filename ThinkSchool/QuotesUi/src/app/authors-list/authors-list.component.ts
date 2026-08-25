import { Component, computed, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppError } from '../core/quotes-api.models';

/** Shape of one entry from GET /api/quotes/slow-authors — a real, unauthenticated QuotesApi endpoint. */
export interface AuthorSummary {
  author: string;
  count: number;
  quotes: string[];
}

@Component({
  selector: 'app-authors-list',
  standalone: true,
  templateUrl: './authors-list.component.html',
  styleUrl: './authors-list.component.css'
})
export class AuthorsListComponent {
  private readonly http = inject(HttpClient);

  protected readonly authors = signal<AuthorSummary[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly expandedAuthor = signal<string | null>(null);

  /** Derived from the authors signal: total quotes across every author. */
  protected readonly totalQuotes = computed(() =>
    this.authors().reduce((sum, a) => sum + a.count, 0)
  );

  protected readonly viewState = computed<'loading' | 'error' | 'empty' | 'success'>(() => {
    if (this.loading()) return 'loading';
    if (this.error()) return 'error';
    if (this.authors().length === 0) return 'empty';
    return 'success';
  });

  constructor() {
    effect(() => {
      this.fetchAuthors();
    });
  }

  protected retry(): void {
    this.fetchAuthors();
  }

  protected toggle(author: string): void {
    this.expandedAuthor.update((current) => (current === author ? null : author));
  }

  private fetchAuthors(): void {
    this.loading.set(true);
    this.error.set(null);

    this.http.get<AuthorSummary[]>('/api/quotes/slow-authors').subscribe({
      next: (response) => {
        this.authors.set(response);
        this.loading.set(false);
      },
      error: (err: AppError) => {
        this.error.set(err.message);
        this.loading.set(false);
      }
    });
  }
}

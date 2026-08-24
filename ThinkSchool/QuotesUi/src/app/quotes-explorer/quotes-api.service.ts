import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

/**
 * A single quote, typed exactly to the real QuotesApi field names
 * (`ThinkSchool/QuotesApi/Models/Quote.cs`, serialized as camelCase JSON).
 * Only the fields this exercise uses are modeled — the real API also
 * returns `createdByUserId`, which is intentionally omitted here rather
 * than invented or renamed.
 */
export interface Quote {
  id: number;
  author: string;
  text: string;
}

/** The real GET /api/quotes envelope shape — page/size/total/items, verbatim. */
export interface QuotesListResponse {
  page: number;
  size: number;
  total: number;
  items: Quote[];
}

@Injectable({ providedIn: 'root' })
export class QuotesApiService {
  private readonly http = inject(HttpClient);

  /** GET /api/quotes?page={page}&size={size} */
  getQuotes(page: number, size: number): Observable<QuotesListResponse> {
    const params = new HttpParams().set('page', page).set('size', size);
    return this.http.get<QuotesListResponse>('/api/quotes', { params });
  }

  /** GET /api/quotes/{id} */
  getQuoteById(id: number): Observable<Quote> {
    return this.http.get<Quote>(`/api/quotes/${id}`);
  }
}

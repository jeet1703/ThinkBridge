/** A single quote — matches `ThinkSchool/QuotesApi/Models/Quote.cs` field-for-field (camelCase JSON). */
export interface Quote {
  id: number;
  author: string;
  text: string;
  createdByUserId: number | null;
}

/** The real GET /api/quotes envelope — page/size/total/items, verbatim. */
export interface QuotesListResponse {
  page: number;
  size: number;
  total: number;
  items: Quote[];
}

/** The real plain-ProblemDetails shape ASP.NET Core returns for e.g. a 404 (`Results.NotFound(new ProblemDetails{...})`). */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
}

/** The real shape for a 400 from `Results.ValidationProblem(...)` — a ProblemDetails plus a field->messages map. */
export interface ValidationProblemDetails extends ProblemDetails {
  errors: Record<string, string[]>;
}

/**
 * A typed, friendly error every component can rely on instead of a raw HttpErrorResponse —
 * produced by `errorMappingInterceptor` from the real ProblemDetails/ValidationProblemDetails
 * shapes above.
 */
export interface AppError {
  kind: 'validation' | 'not-found' | 'forbidden' | 'server' | 'network' | 'unknown';
  message: string;
  status?: number;
  /** Only present for `kind: 'validation'` — the real field->messages map from ValidationProblemDetails. */
  fieldErrors?: Record<string, string[]>;
}

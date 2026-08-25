import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { AppError, ProblemDetails, ValidationProblemDetails } from '../quotes-api.models';

function isValidationProblemDetails(body: unknown): body is ValidationProblemDetails {
  return typeof body === 'object' && body !== null && 'errors' in body && typeof (body as ProblemDetails & { errors?: unknown }).errors === 'object';
}

function toAppError(err: unknown): AppError {
  if (!(err instanceof HttpErrorResponse)) {
    return { kind: 'unknown', message: 'An unexpected error occurred.' };
  }

  if (err.status === 0) {
    return { kind: 'network', message: 'Could not reach the server. Check your connection and try again.' };
  }

  if (err.status === 400 && isValidationProblemDetails(err.error)) {
    return {
      kind: 'validation',
      message: 'Please fix the highlighted fields and try again.',
      status: 400,
      fieldErrors: err.error.errors
    };
  }

  if (err.status === 404) {
    const body = err.error as ProblemDetails | null;
    return { kind: 'not-found', message: body?.detail ?? 'The requested resource was not found.', status: 404 };
  }

  if (err.status === 401 || err.status === 403) {
    return {
      kind: 'forbidden',
      message: "You don't have permission to do that. Sign in with an editor account and try again.",
      status: err.status
    };
  }

  if (err.status >= 500) {
    return { kind: 'server', message: 'Something went wrong on the server. Please try again.', status: err.status };
  }

  return { kind: 'unknown', message: `Request failed (HTTP ${err.status}).`, status: err.status };
}

/**
 * Placed BEFORE retryInterceptor in the provideHttpClient() array — Angular's functional
 * interceptors unwind responses in REVERSE array order, so the LAST interceptor in the array
 * is closest to the backend and sees a failed response first. retryInterceptor must be last
 * (closest to the backend) so it operates on the real HttpErrorResponse and retries transient
 * failures; this interceptor then only maps whatever error survives that — the FINAL one,
 * once retries are exhausted — never an in-flight retryable one.
 */
export const errorMappingInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(catchError((err) => throwError(() => toAppError(err))));

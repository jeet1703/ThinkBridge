import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { retry, throwError, timer } from 'rxjs';

/** Idempotent HTTP methods eligible for a transparent retry — a failed write must never be replayed automatically. */
const IDEMPOTENT_METHODS = new Set(['GET', 'HEAD']);

const MAX_RETRIES = 2;
const BASE_DELAY_MS = 300;

function isTransportOrServerError(error: unknown): boolean {
  // status 0 = the request never reached a server (offline, DNS, connection refused, CORS block).
  // 5xx = the server itself failed. Neither implies the request was bad, so both are worth retrying.
  // A 4xx is never retried here — the request itself was rejected, and repeating it verbatim won't change that.
  return error instanceof HttpErrorResponse && (error.status === 0 || error.status >= 500);
}

/** Retries idempotent GET/HEAD requests with exponential backoff (300ms, 600ms, ...) on transport/5xx failures only. */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (!IDEMPOTENT_METHODS.has(req.method)) {
    return next(req);
  }

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error, retryCount) => {
        if (!isTransportOrServerError(error)) {
          return throwError(() => error);
        }
        return timer(BASE_DELAY_MS * 2 ** (retryCount - 1));
      }
    })
  );
};

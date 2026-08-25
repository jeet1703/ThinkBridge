import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { AppError } from '../quotes-api.models';
import { errorMappingInterceptor } from './error-mapping.interceptor';
import { retryInterceptor } from './retry.interceptor';

/**
 * A regression guard for a real composition bug caught while wiring these two interceptors
 * together in app.config.ts: Angular's functional interceptors unwind the RESPONSE in the
 * REVERSE of the array order used for the REQUEST — the interceptor listed LAST is closest to
 * the backend and sees a failed response first. A natural-looking (but wrong) reading order —
 * `[retryInterceptor, errorMappingInterceptor]` — actually puts errorMappingInterceptor closest
 * to the backend, so it converts every failure into an AppError before retryInterceptor ever
 * gets to inspect it as an HttpErrorResponse. Since retryInterceptor's retry condition checks
 * `error instanceof HttpErrorResponse`, that check silently always fails once the error has
 * already been mapped — retry becomes a no-op with no error, no warning, just one attempt.
 */
describe('retry + error-mapping interceptor composition order', () => {
  let httpMock: HttpTestingController;

  afterEach(() => httpMock.verify());

  it('WRONG order — retryInterceptor before errorMappingInterceptor — never actually retries', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([retryInterceptor, errorMappingInterceptor])),
        provideHttpClientTesting()
      ]
    });
    const http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);

    const promise = firstValueFrom(http.get<unknown>('/api/quotes')).catch((e) => e as AppError);
    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });

    // If retry had worked, there would be a second request to flush. There isn't one.
    httpMock.expectNone('/api/quotes');

    const result = (await promise) as AppError;
    expect(result.kind).toBe('server'); // already a mapped AppError after just one attempt
  });

  it('CORRECT order (matches app.config.ts) — errorMappingInterceptor before retryInterceptor — actually retries', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorMappingInterceptor, retryInterceptor])),
        provideHttpClientTesting()
      ]
    });
    const http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);

    vi.useFakeTimers();
    const promise = firstValueFrom(http.get('/api/quotes'));

    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(300);
    httpMock.expectOne('/api/quotes').flush({ page: 1, size: 5, total: 0, items: [] }); // the retried request succeeds

    expect(await promise).toEqual({ page: 1, size: 5, total: 0, items: [] });
    vi.useRealTimers();
  });
});

import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { retryInterceptor } from './retry.interceptor';

describe('retryInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([retryInterceptor])), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  it('retries a GET on a transient 503, with backoff, and succeeds once the server recovers', async () => {
    vi.useFakeTimers();
    const promise = firstValueFrom(http.get('/api/quotes'));

    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(300); // 1st backoff: 300ms

    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(600); // 2nd backoff: 600ms (exponential)

    httpMock.expectOne('/api/quotes').flush({ page: 1, size: 5, total: 0, items: [] });

    expect(await promise).toEqual({ page: 1, size: 5, total: 0, items: [] });
  });

  it('gives up after exhausting retries against a persistently-down server', async () => {
    vi.useFakeTimers();
    const promise = firstValueFrom(http.get('/api/quotes')).catch((err) => err);

    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(300);
    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });
    await vi.advanceTimersByTimeAsync(600);
    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' }); // 3rd = final attempt

    const result = await promise;
    expect(result.status).toBe(503); // still the real error — no 4th attempt
    httpMock.expectNone('/api/quotes');
  });

  it('does NOT retry a real 400 (the request itself is bad, not the server)', async () => {
    const promise = firstValueFrom(http.get('/api/quotes')).catch((err) => err);
    httpMock.expectOne('/api/quotes').flush('bad request', { status: 400, statusText: 'Bad Request' });

    const result = await promise;
    expect(result.status).toBe(400);
    httpMock.expectNone('/api/quotes'); // confirms no retry attempt was made
  });

  it('does NOT retry a non-idempotent POST, even on a 503', async () => {
    const promise = firstValueFrom(http.post('/api/quotes', { author: 'A', text: 'B' })).catch((err) => err);
    httpMock.expectOne('/api/quotes').flush('down', { status: 503, statusText: 'Service Unavailable' });

    const result = await promise;
    expect(result.status).toBe(503);
    httpMock.expectNone('/api/quotes'); // a POST must never be silently replayed
  });
});

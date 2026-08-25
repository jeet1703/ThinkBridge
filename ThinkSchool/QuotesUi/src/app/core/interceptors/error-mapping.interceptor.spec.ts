import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { AppError } from '../quotes-api.models';
import { errorMappingInterceptor } from './error-mapping.interceptor';

describe('errorMappingInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([errorMappingInterceptor])), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('maps a real 400 ValidationProblemDetails (page validation) to a typed validation AppError', async () => {
    const promise = firstValueFrom(http.get('/api/quotes'));
    httpMock.expectOne('/api/quotes').flush(
      {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        title: 'One or more validation errors occurred.',
        status: 400,
        errors: { page: ['Page must be greater than 0.'] }
      },
      { status: 400, statusText: 'Bad Request' }
    );

    const error = (await promise.catch((e) => e)) as AppError;
    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({ page: ['Page must be greater than 0.'] });
    expect(error.message).toContain('fix the highlighted');
  });

  it('maps a real 404 ProblemDetails (quote not found) to a typed not-found AppError using its `detail`', async () => {
    const promise = firstValueFrom(http.get('/api/quotes/999999999'));
    httpMock.expectOne('/api/quotes/999999999').flush(
      { type: 'about:blank', title: 'Quote not found.', status: 404, detail: 'Quote 999999999 was not found.' },
      { status: 404, statusText: 'Not Found' }
    );

    const error = (await promise.catch((e) => e)) as AppError;
    expect(error.kind).toBe('not-found');
    expect(error.message).toBe('Quote 999999999 was not found.');
    expect(error.fieldErrors).toBeUndefined();
  });

  it('maps a real 401 (unauthenticated POST /api/quotes) to a typed forbidden AppError with a friendly message', async () => {
    const promise = firstValueFrom(http.post('/api/quotes', { author: 'A', text: 'B' }));
    httpMock.expectOne('/api/quotes').flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    const error = (await promise.catch((e) => e)) as AppError;
    expect(error.kind).toBe('forbidden');
    expect(error.message).toContain("don't have permission");
  });

  it('maps a 500 to a typed server AppError', async () => {
    const promise = firstValueFrom(http.get('/api/quotes'));
    httpMock.expectOne('/api/quotes').flush('boom', { status: 500, statusText: 'Internal Server Error' });

    const error = (await promise.catch((e) => e)) as AppError;
    expect(error.kind).toBe('server');
    expect(error.status).toBe(500);
  });

  it('maps a network failure (status 0) to a typed network AppError', async () => {
    const promise = firstValueFrom(http.get('/api/quotes'));
    httpMock.expectOne('/api/quotes').error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

    const error = (await promise.catch((e) => e)) as AppError;
    expect(error.kind).toBe('network');
  });
});

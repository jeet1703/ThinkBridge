import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ProblemDetails, QuotesListResponse, ValidationProblemDetails } from './quotes-api.models';

/**
 * CHARACTERIZATION TEST — pins the real Week-1 QuotesApi contract by hitting the actual,
 * locally-running API (`dotnet run` on http://localhost:5152), not a mock. Written and made
 * green BEFORE any interceptor or UI code, so the interceptors built afterward target an
 * already-verified contract rather than an assumed one.
 *
 * Requires the real API running locally (`dotnet run` in ThinkSchool/QuotesApi) AND its
 * "LocalDev" CORS policy (`InfrastructureExtensions.cs` / `Program.cs`) — these make REAL,
 * cross-origin network calls via provideHttpClient(), not HttpTestingController mocks.
 */
const API_BASE = 'http://localhost:5152';

describe('CHARACTERIZATION: real QuotesApi contract', () => {
  let http: HttpClient;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient()] });
    http = TestBed.inject(HttpClient);
  });

  it('GET /api/quotes?page=1&size=5 returns the real envelope + item shape', async () => {
    const response = await firstValueFrom(
      http.get<QuotesListResponse>(`${API_BASE}/api/quotes`, { params: { page: 1, size: 5 } })
    );

    expect(typeof response.page).toBe('number');
    expect(typeof response.size).toBe('number');
    expect(typeof response.total).toBe('number');
    expect(Array.isArray(response.items)).toBe(true);
    expect(response.items.length).toBe(5);

    for (const item of response.items) {
      expect(typeof item.id).toBe('number');
      expect(typeof item.author).toBe('string');
      expect(typeof item.text).toBe('string');
      expect(item.createdByUserId === null || typeof item.createdByUserId === 'number').toBe(true);
    }
  });

  it('GET /api/quotes?page=0&size=5 (invalid) returns a real 400 ValidationProblemDetails', async () => {
    let caught: HttpErrorResponse | undefined;
    try {
      await firstValueFrom(http.get(`${API_BASE}/api/quotes`, { params: { page: 0, size: 5 } }));
    } catch (err) {
      caught = err as HttpErrorResponse;
    }

    expect(caught).toBeInstanceOf(HttpErrorResponse);
    expect(caught!.status).toBe(400);

    const body = caught!.error as ValidationProblemDetails;
    expect(body.status).toBe(400);
    expect(body.errors).toBeTruthy();
    expect(body.errors['page']).toEqual(['Page must be greater than 0.']);
  });

  it('GET /api/quotes/{id} for a real, present quote returns a single Quote object (not the list envelope)', async () => {
    const list = await firstValueFrom(
      http.get<QuotesListResponse>(`${API_BASE}/api/quotes`, { params: { page: 1, size: 1 } })
    );
    const knownId = list.items[0].id;

    const single = await firstValueFrom(http.get<Record<string, unknown>>(`${API_BASE}/api/quotes/${knownId}`));
    expect(single['id']).toBe(knownId);
    expect(typeof single['author']).toBe('string');
    expect(typeof single['text']).toBe('string');
    expect(single['items']).toBeUndefined(); // pins that this is NOT the paged envelope shape
  });

  it('GET /api/quotes/{id} for a missing id returns a real 404 with plain ProblemDetails (no `errors` map)', async () => {
    let caught: HttpErrorResponse | undefined;
    try {
      await firstValueFrom(http.get(`${API_BASE}/api/quotes/999999999`));
    } catch (err) {
      caught = err as HttpErrorResponse;
    }

    expect(caught).toBeInstanceOf(HttpErrorResponse);
    expect(caught!.status).toBe(404);

    const body = caught!.error as ProblemDetails;
    expect(body.status).toBe(404);
    expect(typeof body.detail).toBe('string');
    expect((body as ValidationProblemDetails).errors).toBeUndefined(); // pins: 404 body has no `errors` map, unlike 400
  });
});

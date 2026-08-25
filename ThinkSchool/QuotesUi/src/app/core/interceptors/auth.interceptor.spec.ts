import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthTokenStore } from '../auth-token.store';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let tokenStore: AuthTokenStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()]
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStore = TestBed.inject(AuthTokenStore);
  });

  afterEach(() => httpMock.verify());

  it('does not attach an Authorization header when no token is set', () => {
    http.get('/api/quotes').subscribe();
    const req = httpMock.expectOne('/api/quotes');
    expect(req.request.headers.has('Authorization')).toBe(false);
    req.flush({});
  });

  it('attaches Authorization: Bearer <token> when a token is set', () => {
    tokenStore.setToken('real-jwt-value');
    http.get('/api/quotes').subscribe();
    const req = httpMock.expectOne('/api/quotes');
    expect(req.request.headers.get('Authorization')).toBe('Bearer real-jwt-value');
    req.flush({});
  });
});

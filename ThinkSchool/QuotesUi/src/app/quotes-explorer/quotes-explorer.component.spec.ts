import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { QuotesExplorerComponent } from './quotes-explorer.component';
import { Quote, QuotesListResponse } from './quotes-api.service';

const LIST_RESPONSE: QuotesListResponse = {
  page: 1,
  size: 5,
  total: 2,
  items: [
    { id: 1, author: 'Marcus Aurelius', text: 'You have power over your mind.' },
    { id: 2, author: 'Seneca', text: 'It is not that we have a short time to live.' }
  ]
};

describe('QuotesExplorerComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuotesExplorerComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads the list with the real page=1&size=5 params and renders it', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    expect(el.querySelector('.state--loading')).toBeTruthy();

    const req = httpMock.expectOne((r) => r.url === '/api/quotes');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('size')).toBe('5');
    req.flush(LIST_RESPONSE);
    await fixture.whenStable();

    const cards = el.querySelectorAll('.quote-card');
    expect(cards.length).toBe(2);
    expect(el.textContent).toContain('Marcus Aurelius');
  });

  it('renders the empty state when the list has no items', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock.expectOne((r) => r.url === '/api/quotes').flush({ page: 1, size: 5, total: 0, items: [] } satisfies QuotesListResponse);
    await fixture.whenStable();

    expect(el.querySelector('.explorer__list .state--empty')).toBeTruthy();
  });

  it('renders the list error state and recovers on retry', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock.expectOne((r) => r.url === '/api/quotes').flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(el.querySelector('.explorer__list .state--error')?.textContent).toContain('HTTP 500');

    (el.querySelector('.explorer__list .state--error button') as HTMLButtonElement).click();
    await fixture.whenStable();

    httpMock.expectOne((r) => r.url === '/api/quotes').flush(LIST_RESPONSE);
    await fixture.whenStable();

    expect(el.querySelectorAll('.quote-card').length).toBe(2);
  });

  it('shows an idle detail panel until a quote is selected, then fetches GET /api/quotes/{id}', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();
    httpMock.expectOne((r) => r.url === '/api/quotes').flush(LIST_RESPONSE);
    await fixture.whenStable();

    expect(el.querySelector('.explorer__detail .state--empty')?.textContent).toContain('Select a quote');

    (el.querySelectorAll('.quote-card')[0] as HTMLLIElement).click();
    await fixture.whenStable();

    const detailReq = httpMock.expectOne('/api/quotes/1');
    detailReq.flush({ id: 1, author: 'Marcus Aurelius', text: 'You have power over your mind.' } satisfies Quote);
    await fixture.whenStable();

    expect(el.querySelector('.detail')?.textContent).toContain('Marcus Aurelius');
  });

  it('renders the detail error state (e.g. a 404) with a working retry', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();
    httpMock.expectOne((r) => r.url === '/api/quotes').flush(LIST_RESPONSE);
    await fixture.whenStable();

    (el.querySelectorAll('.quote-card')[0] as HTMLLIElement).click();
    await fixture.whenStable();

    httpMock.expectOne('/api/quotes/1').flush('not found', { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(el.querySelector('.explorer__detail .state--error')?.textContent).toContain('not found');

    (el.querySelector('.explorer__detail .state--error button') as HTMLButtonElement).click();
    await fixture.whenStable();

    httpMock.expectOne('/api/quotes/1').flush({ id: 1, author: 'Marcus Aurelius', text: 'You have power over your mind.' } satisfies Quote);
    await fixture.whenStable();

    expect(el.querySelector('.detail')?.textContent).toContain('Marcus Aurelius');
  });

  it('discards a stale detail response: clicking quote B before quote A resolves must show B, even if A resolves last', async () => {
    const fixture = TestBed.createComponent(QuotesExplorerComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();
    httpMock.expectOne((r) => r.url === '/api/quotes').flush(LIST_RESPONSE);
    await fixture.whenStable();

    const cards = el.querySelectorAll('.quote-card');

    // Click quote A (id=1) — its request is now in flight.
    (cards[0] as HTMLLIElement).click();
    await fixture.whenStable();
    const reqA = httpMock.expectOne('/api/quotes/1');

    // Before A resolves, click quote B (id=2) — a second request is now also in flight.
    (cards[1] as HTMLLIElement).click();
    await fixture.whenStable();
    const reqB = httpMock.expectOne('/api/quotes/2');

    // Resolve out of order: B (the newer request) resolves FIRST...
    reqB.flush({ id: 2, author: 'Seneca', text: 'It is not that we have a short time to live.' } satisfies Quote);
    await fixture.whenStable();

    // ...then A (the stale, older request) resolves LAST.
    reqA.flush({ id: 1, author: 'Marcus Aurelius', text: 'You have power over your mind.' } satisfies Quote);
    await fixture.whenStable();

    // The panel must still show B — A's late response must be discarded, not overwrite the newer selection.
    const detailText = el.querySelector('.detail')?.textContent ?? '';
    expect(detailText).toContain('Seneca');
    expect(detailText).not.toContain('Marcus Aurelius');
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { QuotesListComponent, QuotesResponse } from './quotes-list.component';

describe('QuotesListComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [QuotesListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows the loading state, then renders the list on success', async () => {
    const fixture = TestBed.createComponent(QuotesListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.querySelector('.state--loading')).toBeTruthy();

    const req = httpMock.expectOne((r) => r.url === '/api/quotes');
    expect(req.request.params.get('page')).toBe('1');
    expect(req.request.params.get('size')).toBe('10');

    const response: QuotesResponse = {
      page: 1,
      size: 10,
      total: 25,
      items: [
        { id: 1, author: 'Marcus Aurelius', text: 'You have power over your mind.', createdByUserId: null },
        { id: 2, author: 'Seneca', text: 'It is not that we have a short time to live.', createdByUserId: null }
      ]
    };
    req.flush(response);
    await fixture.whenStable();

    const items = el.querySelectorAll('.quote-card');
    expect(items.length).toBe(2);
    expect(el.textContent).toContain('Marcus Aurelius');
    // computed(): totalPages derived from total (25) + pageSize (10) signals -> ceil(25/10) = 3
    expect(el.querySelector('.pagination span')?.textContent).toContain('Page 1 of 3');
  });

  it('renders the empty state when the API returns items: []', async () => {
    const fixture = TestBed.createComponent(QuotesListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush({ page: 1, size: 10, total: 0, items: [] } satisfies QuotesResponse);
    await fixture.whenStable();

    expect(el.querySelector('.state--empty')).toBeTruthy();
    expect(el.querySelector('.quote-card')).toBeFalsy();
  });

  it('renders the error state on a failed request, and recovers on retry', async () => {
    const fixture = TestBed.createComponent(QuotesListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    const errorEl = el.querySelector('.state--error');
    expect(errorEl?.textContent).toContain('HTTP 500');

    (errorEl?.querySelector('button') as HTMLButtonElement).click();
    await fixture.whenStable();

    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush({
        page: 1,
        size: 10,
        total: 1,
        items: [{ id: 9, author: 'Epictetus', text: 'It is not events that disturb people.', createdByUserId: null }]
      } satisfies QuotesResponse);
    await fixture.whenStable();

    expect(el.querySelector('.quote-card')).toBeTruthy();
    expect(el.querySelector('.state--error')).toBeFalsy();
  });

  it('recomputes totalPages when the total or pageSize signal changes', async () => {
    const fixture = TestBed.createComponent(QuotesListComponent);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const component = fixture.componentInstance as any;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush({ page: 1, size: 10, total: 95, items: [] } satisfies QuotesResponse);
    await fixture.whenStable();

    expect(component.totalPages()).toBe(10); // ceil(95 / 10)

    component.pageSize.set(20);
    await fixture.whenStable();

    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush({ page: 1, size: 20, total: 95, items: [] } satisfies QuotesResponse);
    await fixture.whenStable();

    expect(component.totalPages()).toBe(5); // ceil(95 / 20)
  });

  it('debounces search input, resets to page 1, and sends a real `search` param', async () => {
    const fixture = TestBed.createComponent(QuotesListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    // initial load
    httpMock
      .expectOne((r) => r.url === '/api/quotes')
      .flush({ page: 1, size: 10, total: 3, items: [{ id: 1, author: 'A', text: 'x', createdByUserId: null }] } satisfies QuotesResponse);
    await fixture.whenStable();

    const input = el.querySelector('.search-input') as HTMLInputElement;
    input.value = 'Marcus';
    input.dispatchEvent(new Event('input'));

    // no request until the debounce elapses
    httpMock.expectNone((r) => r.url === '/api/quotes' && r.params.has('search'));

    await new Promise((resolve) => setTimeout(resolve, 350));
    await fixture.whenStable();

    const req = httpMock.expectOne((r) => r.url === '/api/quotes' && r.params.has('search'));
    expect(req.request.params.get('search')).toBe('Marcus');
    expect(req.request.params.get('page')).toBe('1'); // reset even if a later page was active

    req.flush({
      page: 1,
      size: 10,
      total: 1,
      items: [{ id: 42, author: 'Marcus Aurelius', text: 'You have power over your mind.', createdByUserId: null }]
    } satisfies QuotesResponse);
    await fixture.whenStable();

    expect(el.textContent).toContain('Marcus Aurelius');
  });
});

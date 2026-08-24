import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthorsListComponent, AuthorSummary } from './authors-list.component';

describe('AuthorsListComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuthorsListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders authors and expands to show their quotes on click', async () => {
    const fixture = TestBed.createComponent(AuthorsListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    const response: AuthorSummary[] = [
      { author: 'Author 001', count: 2, quotes: ['Quote one.', 'Quote two.'] },
      { author: 'Author 002', count: 1, quotes: ['Solo quote.'] }
    ];
    httpMock.expectOne('/api/quotes/slow-authors').flush(response);
    await fixture.whenStable();

    const cards = el.querySelectorAll('.author-card');
    expect(cards.length).toBe(2);
    // computed(): totalQuotes derived from the authors signal -> 2 + 1 = 3
    expect(el.querySelector('.authors__count')?.textContent).toContain('3 quotes');
    expect(el.querySelector('.author-card__quotes')).toBeFalsy();

    (cards[0].querySelector('.author-card__header') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(el.querySelector('.author-card__quotes')?.textContent).toContain('Quote one.');
  });

  it('renders the error state on a failed request', async () => {
    const fixture = TestBed.createComponent(AuthorsListComponent);
    const el: HTMLElement = fixture.nativeElement;

    fixture.detectChanges();
    await fixture.whenStable();

    httpMock.expectOne('/api/quotes/slow-authors').flush('boom', { status: 500, statusText: 'Server Error' });
    await fixture.whenStable();

    expect(el.querySelector('.state--error')?.textContent).toContain('HTTP 500');
  });
});

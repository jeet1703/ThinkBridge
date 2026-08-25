import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CreateQuoteFormSignalsComponent, Quote } from './create-quote-form-signals.component';
import { errorMappingInterceptor } from '../core/interceptors/error-mapping.interceptor';

function type(el: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  el.value = value;
  el.dispatchEvent(new Event('input'));
}

describe('CreateQuoteFormSignalsComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateQuoteFormSignalsComponent],
      providers: [provideHttpClient(withInterceptors([errorMappingInterceptor])), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('starts pristine/untouched with no errors shown', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('.field__error')).toBeFalsy();
    expect((el.querySelector('#author') as HTMLInputElement).getAttribute('aria-invalid')).toBe('false');
  });

  it('required() alone does NOT catch a whitespace-only value — validate() does (matches the real API)', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, '   ');
    (el.querySelector('#author') as HTMLInputElement).dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect((el.querySelector('#author') as HTMLInputElement).getAttribute('aria-invalid')).toBe('true');
  });

  it('marks fields dirty/touched, shows errors, and moves focus to the first invalid field on submit', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    (el.querySelector('form') as HTMLFormElement).requestSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    const author = el.querySelector('#author') as HTMLInputElement;
    expect(author.getAttribute('aria-invalid')).toBe('true');
    expect(el.querySelector('#author-error')?.textContent).toContain('Author is required.');
    expect(document.activeElement).toBe(author);

    httpMock.expectNone((r) => r.url === '/api/quotes');
  });

  it('a clean submit shows submitting then the real success panel', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, 'Marcus Aurelius');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'You have power over your mind.');
    (el.querySelector('form') as HTMLFormElement).requestSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/quotes');
    expect(req.request.body).toEqual({ author: 'Marcus Aurelius', text: 'You have power over your mind.' });
    req.flush({ id: 42, author: 'Marcus Aurelius', text: 'You have power over your mind.', createdByUserId: 1 } satisfies Quote);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('.panel--success')?.textContent).toContain('Marcus Aurelius');
    expect(el.querySelector('form')).toBeFalsy();
  });

  it('a failed submit (401) shows a banner AND a later retry still actually re-submits — this was the bug', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, 'Marcus Aurelius');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'You have power over your mind.');
    (el.querySelector('form') as HTMLFormElement).requestSubmit();

    httpMock.expectOne('/api/quotes').flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('.submit-error')?.textContent).toContain("don't have permission");

    // Retry: this must actually hit the network again, not silently no-op.
    (el.querySelector('form') as HTMLFormElement).requestSubmit();
    const retryReq = httpMock.expectOne('/api/quotes'); // throws if no second request was made
    retryReq.flush({ id: 43, author: 'Marcus Aurelius', text: 'You have power over your mind.', createdByUserId: 1 } satisfies Quote);
    await fixture.whenStable();

    expect(el.querySelector('.panel--success')?.textContent).toContain('Marcus Aurelius');
  });

  it('maps a real 400 ValidationProblem onto the matching field, and it clears once fixed', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormSignalsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, 'Valid Author');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'Valid text');
    (el.querySelector('form') as HTMLFormElement).requestSubmit();

    httpMock.expectOne('/api/quotes').flush(
      { errors: { author: ['Author is required.'] } },
      { status: 400, statusText: 'Bad Request' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('#author-error')?.textContent).toContain('Author is required.');

    type(el.querySelector('#author') as HTMLInputElement, 'A Fixed Author');
    fixture.detectChanges();
    await fixture.whenStable();

    expect((el.querySelector('#author') as HTMLInputElement).getAttribute('aria-invalid')).toBe('false');
  });
});

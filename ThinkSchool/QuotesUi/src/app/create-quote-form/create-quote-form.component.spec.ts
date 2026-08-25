import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { CreateQuoteFormComponent, Quote } from './create-quote-form.component';

function type(el: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  el.value = value;
  el.dispatchEvent(new Event('input'));
}

describe('CreateQuoteFormComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateQuoteFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('starts empty with no errors shown and every input labeled', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    expect(el.querySelector('.field__error')).toBeFalsy();

    const author = el.querySelector('#author') as HTMLInputElement;
    const text = el.querySelector('#text') as HTMLTextAreaElement;
    expect(el.querySelector('label[for="author"]')?.textContent).toContain('Author');
    expect(el.querySelector('label[for="text"]')?.textContent).toContain('Quote text');
    expect(author.getAttribute('aria-invalid')).toBe('false');
    expect(text.getAttribute('aria-invalid')).toBe('false');
  });

  it('rejects a whitespace-only author (matching the API\'s IsNullOrWhiteSpace check) while accepting short text (the API enforces no min length)', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, '   ');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'hi'); // short — the real API accepts this
    (el.querySelector('#author') as HTMLInputElement).dispatchEvent(new Event('blur'));
    (el.querySelector('#text') as HTMLTextAreaElement).dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    await fixture.whenStable();

    expect((el.querySelector('#author') as HTMLInputElement).getAttribute('aria-invalid')).toBe('true');
    expect((el.querySelector('#text') as HTMLTextAreaElement).getAttribute('aria-invalid')).toBe('false');
  });

  it('shows validation errors and moves focus to the first invalid field on submit', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    (el.querySelector('form') as HTMLFormElement).requestSubmit();
    await fixture.whenStable();
    fixture.detectChanges();

    const author = el.querySelector('#author') as HTMLInputElement;
    const text = el.querySelector('#text') as HTMLTextAreaElement;
    expect(author.getAttribute('aria-invalid')).toBe('true');
    expect(text.getAttribute('aria-invalid')).toBe('true');
    expect(author.getAttribute('aria-describedby')).toBe('author-error');
    expect(el.querySelector('#author-error')?.textContent).toContain('Author is required.');
    expect(document.activeElement).toBe(author);

    httpMock.expectNone((r) => r.url === '/api/quotes'); // never called — invalid submit must not hit the network
  });

  it('submits, shows the submitting state, then the real success panel', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, 'Marcus Aurelius');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'You have power over your mind.');
    (el.querySelector('form') as HTMLFormElement).requestSubmit();
    fixture.detectChanges();

    const submitBtn = el.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submitBtn.disabled).toBe(true);
    expect(el.textContent).toContain('Adding…');

    const req = httpMock.expectOne('/api/quotes');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ author: 'Marcus Aurelius', text: 'You have power over your mind.' });
    req.flush({ id: 42, author: 'Marcus Aurelius', text: 'You have power over your mind.', createdByUserId: 1 } satisfies Quote);
    await fixture.whenStable();

    expect(el.querySelector('.panel--success')?.textContent).toContain('Marcus Aurelius');
    expect(el.querySelector('.panel--success')?.textContent).toContain('id 42');
    expect(el.querySelector('form')).toBeFalsy();
  });

  it('maps a real 400 ValidationProblem onto the matching field and refocuses it', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const el: HTMLElement = fixture.nativeElement;

    type(el.querySelector('#author') as HTMLInputElement, 'Valid Author');
    type(el.querySelector('#text') as HTMLTextAreaElement, 'Valid text');
    (el.querySelector('form') as HTMLFormElement).requestSubmit();

    httpMock.expectOne('/api/quotes').flush(
      { type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1', title: 'One or more validation errors occurred.', status: 400, errors: { author: ['Author is required.'] } },
      { status: 400, statusText: 'Bad Request' }
    );
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('#author-error')?.textContent).toContain('Author is required.');
    expect(el.querySelector('.submit-error')?.textContent).toContain('Please fix the highlighted field');
    expect(document.activeElement).toBe(el.querySelector('#author'));
  });

  it('shows a permission-denied server-error banner on a real 401, and preserves the entered values', async () => {
    const fixture = TestBed.createComponent(CreateQuoteFormComponent);
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
    expect((el.querySelector('#author') as HTMLInputElement).value).toBe('Marcus Aurelius');
    expect((el.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(false);
  });
});

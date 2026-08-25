import { afterNextRender, Component, ElementRef, inject, Injector, signal } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors } from '@angular/forms';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';

/** The real request body for POST /api/quotes — only the fields the API accepts. */
export interface CreateQuoteRequest {
  author: string;
  text: string;
}

/** The real response body for a successful POST /api/quotes (201 Created). */
export interface Quote {
  id: number;
  author: string;
  text: string;
  createdByUserId: number | null;
}

/** The real shape of a 400 response from POST /api/quotes (Results.ValidationProblem). */
interface ValidationProblemBody {
  errors?: Record<string, string[]>;
}

type CreateQuoteFormControls = {
  author: FormControl<string>;
  text: FormControl<string>;
};

/** Submit-time field order — also the order to check when focusing the first invalid one. */
const FIELD_ORDER: Array<keyof CreateQuoteFormControls> = ['author', 'text'];

/** The narrow set of validation-error shapes this form actually produces. */
interface FieldErrors {
  required?: true;
  server?: string;
}

type SubmitStatus = 'idle' | 'submitting' | 'success';

/**
 * Matches the API's actual server-side check (`string.IsNullOrWhiteSpace`) exactly —
 * required AND not just whitespace. Angular's built-in Validators.required does not
 * reject a whitespace-only string, so it can't be used alone here.
 *
 * Deliberately NOT enforcing a min/max length: `CreateQuoteRequestValidator` (3-50 /
 * 5-500 chars) and the `[StringLength]` attributes on `CreateQuoteRequest` both exist
 * in the backend source but are never invoked by this endpoint — confirmed by posting
 * a 2-char author and a 150-char author against the real running API and getting 201
 * back for both. Adding a length validator here would reject real, currently-accepted
 * API input — inventing a rule the API doesn't actually enforce.
 */
function requiredNotBlank(control: AbstractControl<string>): ValidationErrors | null {
  const value = control.value;
  return value == null || value.trim().length === 0 ? { required: true } : null;
}

@Component({
  selector: 'app-create-quote-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './create-quote-form.component.html',
  styleUrl: './create-quote-form.component.css'
})
export class CreateQuoteFormComponent {
  private readonly http = inject(HttpClient);
  private readonly host: ElementRef<HTMLElement> = inject(ElementRef);
  private readonly injector = inject(Injector);

  protected readonly form = new FormGroup<CreateQuoteFormControls>({
    author: new FormControl('', { nonNullable: true, validators: [requiredNotBlank] }),
    text: new FormControl('', { nonNullable: true, validators: [requiredNotBlank] })
  });

  /** Becomes true on the first submit attempt — before that, untouched fields stay quiet. */
  protected readonly submitted = signal(false);
  protected readonly status = signal<SubmitStatus>('idle');
  /** A form-level message: permission/network/unexpected-server problems, not field-level ones. */
  protected readonly submitError = signal<string | null>(null);
  protected readonly createdQuote = signal<Quote | null>(null);

  protected fieldInvalid(name: keyof CreateQuoteFormControls): boolean {
    const control = this.form.controls[name];
    return control.invalid && (control.touched || this.submitted());
  }

  protected fieldErrorId(name: keyof CreateQuoteFormControls): string | null {
    return this.fieldInvalid(name) ? `${name}-error` : null;
  }

  protected fieldErrorMessage(name: keyof CreateQuoteFormControls): string {
    const errors = this.form.controls[name].errors as FieldErrors | null;
    if (!errors) return '';
    if (typeof errors.server === 'string') return errors.server;
    if (errors.required) {
      return name === 'author' ? 'Author is required.' : 'Text is required.';
    }
    return 'This field is invalid.';
  }

  protected onSubmit(): void {
    this.submitted.set(true);
    this.submitError.set(null);

    if (this.form.invalid) {
      this.focusFirstInvalidField();
      return;
    }

    this.status.set('submitting');
    this.form.disable();

    const request: CreateQuoteRequest = {
      author: this.form.controls.author.value.trim(),
      text: this.form.controls.text.value.trim()
    };

    this.http.post<Quote>('/api/quotes', request).subscribe({
      next: (created) => {
        this.createdQuote.set(created);
        this.status.set('success');
      },
      error: (err: HttpErrorResponse) => {
        this.form.enable();
        this.status.set('idle');
        this.handleSubmitError(err);
      }
    });
  }

  protected addAnother(): void {
    this.createdQuote.set(null);
    this.status.set('idle');
    this.submitted.set(false);
    this.submitError.set(null);
    this.form.reset({ author: '', text: '' });
  }

  private handleSubmitError(err: HttpErrorResponse): void {
    if (err.status === 400 && this.isValidationProblemBody(err.error)) {
      this.applyServerFieldErrors(err.error.errors);
      this.submitError.set('Please fix the highlighted field(s) below.');
      this.focusFirstInvalidField();
      return;
    }

    if (err.status === 401 || err.status === 403) {
      this.submitError.set("You don't have permission to add quotes. Sign in with an editor account and try again.");
    } else if (err.status === 0) {
      this.submitError.set('Could not reach the Quotes API. Is it running?');
    } else {
      this.submitError.set(`Something went wrong creating the quote (HTTP ${err.status}). Please try again.`);
    }

    this.focusSubmitError();
  }

  private isValidationProblemBody(body: unknown): body is Required<ValidationProblemBody> {
    return (
      typeof body === 'object' &&
      body !== null &&
      'errors' in body &&
      typeof (body as ValidationProblemBody).errors === 'object'
    );
  }

  private applyServerFieldErrors(errors: Record<string, string[]>): void {
    for (const key of Object.keys(errors) as Array<keyof CreateQuoteFormControls>) {
      const control = this.form.controls[key];
      if (control) {
        control.setErrors({ server: errors[key][0] });
        control.markAsTouched();
      }
    }
  }

  /**
   * Looked up by static field id, not by an `aria-invalid="true"` DOM query — the id is
   * always present regardless of whether Angular has re-rendered that attribute yet, so
   * this doesn't have to wait for a render to land first.
   */
  private focusFirstInvalidField(): void {
    const firstInvalidName = FIELD_ORDER.find((name) => this.form.controls[name].invalid);
    if (!firstInvalidName) return;
    this.host.nativeElement.querySelector<HTMLElement>(`#${firstInvalidName}`)?.focus();
  }

  /** The error banner is a newly-rendered element, so this one does need to wait for that render. */
  private focusSubmitError(): void {
    afterNextRender(
      () => {
        this.host.nativeElement.querySelector<HTMLElement>('.submit-error')?.focus();
      },
      { injector: this.injector }
    );
  }
}

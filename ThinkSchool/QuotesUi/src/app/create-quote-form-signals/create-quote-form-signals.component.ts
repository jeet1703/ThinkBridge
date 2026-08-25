import { Component, inject, signal } from '@angular/core';
import { FieldTree, FormField, TreeValidationResult, form, submit, validate } from '@angular/forms/signals';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

/** The real model for POST /api/quotes — only the fields the API accepts. */
export interface CreateQuoteModel {
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

function isValidationProblemBody(body: unknown): body is Required<ValidationProblemBody> {
  return (
    typeof body === 'object' &&
    body !== null &&
    'errors' in body &&
    typeof (body as ValidationProblemBody).errors === 'object'
  );
}

@Component({
  selector: 'app-create-quote-form-signals',
  standalone: true,
  imports: [FormField],
  templateUrl: './create-quote-form-signals.component.html',
  styleUrl: './create-quote-form-signals.component.css'
})
export class CreateQuoteFormSignalsComponent {
  private readonly http = inject(HttpClient);

  protected readonly model = signal<CreateQuoteModel>({ author: '', text: '' });

  /**
   * Same rule as the reactive-forms version, and for the same reason: the real API
   * (`QuoteEndpointExtensions.cs`) only enforces `IsNullOrWhiteSpace` — no min/max length,
   * despite `CreateQuoteRequestValidator` (3-50 / 5-500 chars) existing elsewhere in the
   * backend source and never being wired to this endpoint.
   *
   * Deliberately using `validate()`, not the built-in `required()` — verified empirically
   * that `required()` does not reject a whitespace-only string (same blind spot as Reactive
   * Forms' `Validators.required`), so it can't match `IsNullOrWhiteSpace` on its own. See
   * the verification log.
   */
  protected readonly quoteForm = form(this.model, (path) => {
    validate(path.author, ({ value }) =>
      value().trim().length === 0 ? { kind: 'required', message: 'Author is required.' } : undefined
    );
    validate(path.text, ({ value }) =>
      value().trim().length === 0 ? { kind: 'required', message: 'Text is required.' } : undefined
    );
  });

  protected readonly createdQuote = signal<Quote | null>(null);

  /**
   * NOT wired through Signal Forms' own error tree — see the verification log. Attaching a
   * non-field-specific error to the *root* FieldTree (`fieldTree: field` in the submit action)
   * looked like the "idiomatic" way to report a permission/network failure, but that error
   * never gets cleared or re-evaluated on a later submit — it permanently marks the whole
   * form invalid, and every subsequent submit silently no-ops via onInvalid forever after.
   * A plain signal, reset at the start of every submit attempt, has none of that problem.
   */
  protected readonly submitError = signal<string | null>(null);

  protected async onSubmit(event: SubmitEvent): Promise<void> {
    event.preventDefault();
    this.submitError.set(null);

    await submit(this.quoteForm, {
      action: (field) => this.submitToApi(field),
      onInvalid: () => this.focusFirstInvalidField()
    });
  }

  protected addAnother(): void {
    this.createdQuote.set(null);
    this.submitError.set(null);
    this.quoteForm().reset({ author: '', text: '' });
  }

  private async submitToApi(field: FieldTree<CreateQuoteModel>): Promise<TreeValidationResult> {
    try {
      const created = await firstValueFrom(this.http.post<Quote>('/api/quotes', field().value()));
      this.createdQuote.set(created);
      return undefined;
    } catch (err) {
      if (!(err instanceof HttpErrorResponse)) {
        this.submitError.set('Unexpected error.');
        return undefined;
      }

      if (err.status === 400 && isValidationProblemBody(err.error)) {
        const fieldsByName: Record<string, FieldTree<string>> = {
          author: this.quoteForm.author,
          text: this.quoteForm.text
        };
        return Object.entries(err.error.errors).map(([key, messages]) => ({
          kind: 'server',
          message: messages[0],
          fieldTree: fieldsByName[key] ?? field
        }));
      }

      this.submitError.set(
        err.status === 401 || err.status === 403
          ? "You don't have permission to add quotes. Sign in with an editor account and try again."
          : err.status === 0
            ? 'Could not reach the Quotes API. Is it running?'
            : `Something went wrong creating the quote (HTTP ${err.status}). Please try again.`
      );
      return undefined;
    }
  }

  private focusFirstInvalidField(): void {
    if (this.quoteForm.author().invalid()) {
      this.quoteForm.author().focusBoundControl();
    } else if (this.quoteForm.text().invalid()) {
      this.quoteForm.text().focusBoundControl();
    }
  }
}

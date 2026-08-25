import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection
} from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorMappingInterceptor } from './core/interceptors/error-mapping.interceptor';
import { retryInterceptor } from './core/interceptors/retry.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    // Order matters: interceptors unwind responses in REVERSE array order, so retryInterceptor
    // (last here) is closest to the backend and sees raw failures first — it can retry a
    // transient one before errorMappingInterceptor ever sees it. errorMappingInterceptor then
    // only maps the final, non-retryable (or retry-exhausted) error into a typed AppError.
    provideHttpClient(withInterceptors([authInterceptor, errorMappingInterceptor, retryInterceptor]))
  ]
};

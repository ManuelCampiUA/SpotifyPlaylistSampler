import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

// Suppress benign ResizeObserver notification before Angular's global error listener
window.addEventListener(
  'error',
  (event) => {
    if (event.message?.includes('ResizeObserver loop')) {
      event.stopImmediatePropagation();
    }
  },
  true,
);

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

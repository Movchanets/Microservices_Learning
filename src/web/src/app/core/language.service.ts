import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Locale = 'en' | 'es' | 'fr' | 'uk';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  
  currentLocale = signal<Locale>(this.getInitialLocale());

  constructor() {
    effect(() => {
      const locale = this.currentLocale();
      if (this.isBrowser) {
        localStorage.setItem('locale', locale);
        // Note: For real Angular i18n with @angular/localize, 
        // we might need to redirect to a locale-specific path (e.g. /uk/login)
        // or reload the page if using different bundles.
        // For now, we just update the signal and store it.
      }
    });
  }

  setLocale(locale: Locale) {
    this.currentLocale.set(locale);
    // Optional: implementation for actual locale redirection
    // if (this.isBrowser && locale !== this.getInitialLocale()) {
    //   window.location.reload(); 
    // }
  }

  private getInitialLocale(): Locale {
    if (!this.isBrowser) return 'en';
    return (localStorage.getItem('locale') as Locale) || 'en';
  }
}

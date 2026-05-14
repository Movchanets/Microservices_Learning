import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);

  theme = signal<Theme>(this.getInitialTheme());

  constructor() {
    effect(() => {
      const currentTheme = this.theme();
      this.applyTheme(currentTheme);
      if (this.isBrowser) {
        localStorage.setItem('theme', currentTheme);
      }
    });

    if (this.isBrowser) {
      // Watch for system theme changes if set to 'system'
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        if (this.theme() === 'system') {
          this.applyTheme('system');
        }
      });
    }
  }

  setTheme(theme: Theme) {
    this.theme.set(theme);
  }

  private getInitialTheme(): Theme {
    if (!this.isBrowser) return 'system';
    return (localStorage.getItem('theme') as Theme) || 'system';
  }

  private applyTheme(theme: Theme) {
    if (!this.isBrowser) return;

    const root = document.documentElement;
    let isDark = theme === 'dark';

    if (theme === 'system') {
      isDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    }

    if (isDark) {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }
}

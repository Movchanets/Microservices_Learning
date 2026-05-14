import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ThemeService, Theme } from '../../../core/theme.service';
import { LanguageService, Locale } from '../../../core/language.service';
import { LucideAngularModule } from 'lucide-angular';

@Component({
  selector: 'app-footer',
  imports: [CommonModule, LucideAngularModule],
  standalone: true,
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  themeService = inject(ThemeService);
  langService = inject(LanguageService);

  isThemeOpen = signal(false);
  isLangOpen = signal(false);

  toggleTheme() {
    this.isThemeOpen.update((v) => !v);
    this.isLangOpen.set(false);
  }

  toggleLang() {
    this.isLangOpen.update((v) => !v);
    this.isThemeOpen.set(false);
  }

  setTheme(theme: Theme) {
    this.themeService.setTheme(theme);
    this.isThemeOpen.set(false);
  }

  setLang(lang: Locale) {
    this.langService.setLocale(lang);
    this.isLangOpen.set(false);
  }

  get currentLangName() {
    const lang = this.langService.currentLocale();
    return lang === 'uk' ? 'Українська' : 'English';
  }

  get currentThemeName() {
    const theme = this.themeService.theme();
    return theme.charAt(0).toUpperCase() + theme.slice(1);
  }
}

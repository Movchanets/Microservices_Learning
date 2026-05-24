import { TestBed } from '@angular/core/testing';
import { LanguageService, Locale } from './language.service';
import { PLATFORM_ID } from '@angular/core';

describe('LanguageService', () => {
  let service: LanguageService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        LanguageService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(LanguageService);
  });

  it('should initialize with default English locale', () => {
    expect(service.currentLocale()).toBe('en');
  });

  it('setLocale should update currentLocale and localStorage', () => {
    service.setLocale('es');
    TestBed.flushEffects();

    expect(service.currentLocale()).toBe('es');
    expect(localStorage.getItem('locale')).toBe('es');
  });

  it('getTranslation should return localized string', () => {
    service.setLocale('es');
    expect(service.getTranslation('welcome')).toBe('Bienvenido');

    service.setLocale('fr');
    expect(service.getTranslation('welcome')).toBe('Bienvenue');

    // Fallback behavior if key doesn't exist
    expect(service.getTranslation('unknownKey')).toBe('unknownKey');
  });
});

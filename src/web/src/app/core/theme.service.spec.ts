import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';
import { PLATFORM_ID } from '@angular/core';

describe('ThemeService', () => {
  let service: ThemeService;
  let matchMediaMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    // Reset document element class list
    document.documentElement.className = '';
    localStorage.clear();

    matchMediaMock = vi.fn().mockImplementation((query) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: matchMediaMock,
    });

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });
    service = TestBed.inject(ThemeService);
  });

  it('should initialize with system preference', () => {
    expect(service.theme()).toBe('system');
  });

  it('toggleTheme switches between light and dark', () => {
    service.setTheme('light');
    TestBed.flushEffects();
    expect(service.theme()).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);

    service.toggleTheme();
    TestBed.flushEffects();
    expect(service.theme()).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);

    service.toggleTheme();
    TestBed.flushEffects();
    expect(service.theme()).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('respects window.matchMedia for initial system preference', () => {
    matchMediaMock.mockImplementation((query) => ({
      matches: query === '(prefers-color-scheme: dark)', // Simulate dark mode preference
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));

    service.setTheme('system');
    TestBed.flushEffects();

    // When theme is 'system' and system is dark, root element should have 'dark' class
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });
});

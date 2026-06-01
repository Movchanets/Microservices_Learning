import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideRouter } from '@angular/router';
import { importProvidersFrom } from '@angular/core';
import {
  LucideAngularModule,
  Sun,
  Moon,
  Globe,
  User,
  LogIn,
  Github,
  Mail,
  Lock,
  ChevronDown,
  Monitor,
  Eye,
  EyeOff,
  Search,
  Menu,
  ShoppingCart,
  Heart,
  Settings,
  Clock,
  Package,
} from 'lucide-angular';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        importProvidersFrom(
          LucideAngularModule.pick({
            Sun,
            Moon,
            Globe,
            User,
            LogIn,
            Github,
            Mail,
            Lock,
            ChevronDown,
            Monitor,
            Eye,
            EyeOff,
            Search,
            Menu,
            ShoppingCart,
            Heart,
            Settings,
            Clock,
            Package,
          }),
        ),
      ],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should render header', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-header')).toBeTruthy();
  });
});

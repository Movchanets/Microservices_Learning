import {
  ApplicationConfig,
  PLATFORM_ID,
  inject,
  provideAppInitializer,
  provideBrowserGlobalErrorListeners,
  importProvidersFrom,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { provideRouter } from '@angular/router';
import {
  provideHttpClient,
  withFetch,
  withInterceptors,
  withXsrfConfiguration,
} from '@angular/common/http';
import {
  LucideAngularModule,
  Globe,
  Moon,
  Sun,
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
  ShoppingCart,
  SlidersHorizontal,
  ChevronLeft,
  ChevronRight,
  Tag,
  DollarSign,
  Package,
  Minus,
  Plus,
  Trash2,
  CheckCircle2,
  Clock,
  XCircle,
  AlertTriangle,
  PackageCheck,
  CreditCard,
  ShoppingBag,
  Pencil,
  Settings,
} from 'lucide-angular';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { AuthStore } from './core/auth/auth.store';
import { NotificationService } from './core/signalr/notification.service';
import { apiInterceptor } from './core/http/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiInterceptor]),
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
    ),
    provideAppInitializer(() => {
      if (isPlatformBrowser(inject(PLATFORM_ID))) {
        // Auth must complete before SignalR connects (needs buyerId)
        void inject(AuthStore).checkAuth().then(() => {
          void inject(NotificationService).start();
        });
      }
    }),
    importProvidersFrom(
      LucideAngularModule.pick({
        Globe,
        Moon,
        Sun,
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
        ShoppingCart,
        SlidersHorizontal,
        ChevronLeft,
        ChevronRight,
        Tag,
        DollarSign,
        Package,
        Minus,
        Plus,
        Trash2,
        CheckCircle2,
        Clock,
        XCircle,
        AlertTriangle,
        PackageCheck,
        CreditCard,
        ShoppingBag,
        Pencil,
        Settings,
      }),
    ),
  ],
};

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
import { errorInterceptor } from './core/http/error.interceptor';
import { CategoryTreeService } from './core/services/category-tree.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiInterceptor, errorInterceptor]),
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
    ),
    provideAppInitializer(() => {
      if (isPlatformBrowser(inject(PLATFORM_ID))) {
        const authStore = inject(AuthStore);
        const notificationService = inject(NotificationService);
        const categoryTreeService = inject(CategoryTreeService);
        
        // Fire off category tree load (don't block app boot)
        void categoryTreeService.initialize();

        // Auth must complete before router starts (guards need user state)
        return authStore.checkAuth().then(() => {
          const buyerId = authStore.user()?.id;
          void notificationService.start(buyerId);
        });
      }
      return Promise.resolve();
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

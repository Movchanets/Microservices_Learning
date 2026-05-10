import { ApplicationConfig, PLATFORM_ID, inject, provideAppInitializer, provideBrowserGlobalErrorListeners, importProvidersFrom } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors, withXsrfConfiguration } from '@angular/common/http';
import { LucideAngularModule, Globe, Moon, Sun, User, LogIn, Github, Mail, Lock, ChevronDown, Monitor, Eye, EyeOff } from 'lucide-angular';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { AuthStore } from './core/auth/auth.store';
import { apiInterceptor } from './core/http/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes), 
    provideClientHydration(withEventReplay()),
    provideHttpClient(
      withFetch(),
      withInterceptors([apiInterceptor]),
      withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' })
    ),
    provideAppInitializer(async () => {
      if (isPlatformBrowser(inject(PLATFORM_ID))) {
        await inject(AuthStore).checkAuth();
      }
    }),
    importProvidersFrom(LucideAngularModule.pick({ Globe, Moon, Sun, User, LogIn, Github, Mail, Lock, ChevronDown, Monitor, Eye, EyeOff }))
  ]
};

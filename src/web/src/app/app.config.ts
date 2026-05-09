import { ApplicationConfig, provideBrowserGlobalErrorListeners, importProvidersFrom } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { LucideAngularModule, Globe, Moon, Sun, User, LogIn, Github, Mail, Lock, ChevronDown, Monitor, Eye, EyeOff } from 'lucide-angular';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { apiInterceptor } from './core/http/api.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes), 
    provideClientHydration(withEventReplay()),
    provideHttpClient(withInterceptors([apiInterceptor])),
    importProvidersFrom(LucideAngularModule.pick({ Globe, Moon, Sun, User, LogIn, Github, Mail, Lock, ChevronDown, Monitor, Eye, EyeOff }))
  ]
};

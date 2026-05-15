import { Header } from './shared/components/header/header';
import { Footer } from './shared/components/footer/footer';
import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationBridgeComponent } from './core/signalr/notification-bridge.component';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Header, Footer, NotificationBridgeComponent, ToastContainerComponent],
  template: `
    <app-notification-bridge />
    <app-header></app-header>

    <main class="container mx-auto p-4">
      <router-outlet></router-outlet>
    </main>

    <app-footer></app-footer>
    <app-toast-container />
  `,
})
export class App {}

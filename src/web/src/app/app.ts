import { Header } from './shared/components/header/header';
import { Footer } from './shared/components/footer/footer';
import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationBridgeComponent } from './core/signalr/notification-bridge.component';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container';
import { CartDrawer } from './shared/components/cart-drawer/cart-drawer';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Header, Footer, NotificationBridgeComponent, ToastContainerComponent, CartDrawer],
  template: `
    <app-notification-bridge />
    <app-header></app-header>

    <main class="container mx-auto p-4">
      <router-outlet></router-outlet>
    </main>

    <app-footer></app-footer>
    <app-toast-container />
    <app-cart-drawer />
  `,
})
export class App {}

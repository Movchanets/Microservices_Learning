import { Header } from './shared/components/header/header';
import { Footer } from './shared/components/footer/footer';
import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Header, Footer],
  template: `
    <app-header></app-header>
    
    <main>
      <router-outlet></router-outlet> <!-- Тут будуть ваші сторінки (login, register тощо) -->
    </main>
    
    <app-footer></app-footer>`
})
export class App {
  protected readonly title = signal('web-frontend');
}

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
    
    <main class="container mx-auto p-4">
      <router-outlet></router-outlet>
    </main>
    
    <app-footer></app-footer>`
})
export class App {}

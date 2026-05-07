# Marketplace Frontend Design Guide

This document outlines the core design philosophy, theming, and internationalization guidelines for the Angular 21 frontend of the Marketplace project. **All developers and agents MUST consult this file before creating or modifying UI components.**

## Tech Stack
- **Framework**: Angular 21 (Standalone Components, Signals)
- **CSS Framework**: Tailwind CSS v4
- **UI Components**: `@spartan-ng` (Headless UI for Angular + Tailwind)
- **Package Manager**: `pnpm`

## 1. Theming (Light/Dark Mode)

The application supports both light and dark modes natively. We rely on Tailwind CSS's dark mode capabilities combined with CSS variables managed by `@spartan-ng`.

### Guidelines:
- **Use Semantic Colors**: Always use semantic Tailwind classes provided by the theme configuration rather than hardcoding colors (e.g., use `bg-background` and `text-foreground` instead of `bg-white` and `text-black`).
- **Dark Mode Strategy**: Configure Tailwind to use the `class` strategy for dark mode.
- **Theme Toggle Component**: The user's theme preference should be stored in `localStorage` (or via the BFF session if authenticated) and applied to the root `<html>` element as the `dark` class.
- **System Preference Fallback**: On first load, if no user preference exists, respect the system's `prefers-color-scheme`.

### Example (Tailwind classes):
```html
<div class="bg-card text-card-foreground p-6 rounded-lg shadow-sm border border-border">
  <h2 class="text-xl font-bold">Product Title</h2>
  <p class="text-muted-foreground">Product description goes here.</p>
</div>
```

## 2. Internationalization (i18n)

The marketplace is a global platform and must support multiple languages. We use Angular's built-in `@angular/localize` package.

### Guidelines:
- **No Hardcoded Strings**: Never hardcode user-visible text in templates or TypeScript files.
- **Template i18n**: Use the `i18n` attribute in HTML templates.
  ```html
  <h1 i18n="@@home.welcome">Welcome to the Marketplace</h1>
  ```
- **Code i18n**: Use the `$localize` function for text dynamically generated in TypeScript.
  ```typescript
  const errorMessage = $localize`:@@error.generic:An unexpected error occurred.`;
  ```
- **Date and Currency Formatting**: Use Angular's built-in `DatePipe` and `CurrencyPipe`, which automatically adapt to the user's current locale.
  ```html
  <span>{{ product.price | currency }}</span>
  <span>{{ order.date | date:'short' }}</span>
  ```

## 3. UI Component Construction (@spartan-ng)

We use `@spartan-ng` to construct accessible, unstyled components that we then style with Tailwind CSS.

### Guidelines:
- **Do not invent new interactive primitives**: If you need a dropdown, dialog, accordion, or tabs, look for the `@spartan-ng` equivalent first before attempting to build one from scratch or importing a heavy third-party library.
- **Keep styles localized**: Combine Tailwind utility classes using tools like `clsx` and `tailwind-merge` (typically integrated via `@spartan-ng` utils) to handle conditional class application.

## 4. State Management and Data Binding

- **Signals First**: Use Angular Signals for all synchronous reactive state within components.
- **NgRx SignalStore**: Use SignalStore for complex, multi-component feature states (e.g., shopping cart, product catalog).
- **Control Flow**: Use Angular's new control flow syntax (`@if`, `@for`, `@switch`) exclusively over structural directives (`*ngIf`, `*ngFor`).

```html
@if (user()) {
  <div>Welcome back, {{ user().name }}!</div>
} @else {
  <button (click)="login()">Log In</button>
}
```

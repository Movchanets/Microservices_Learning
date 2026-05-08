# Marketplace Design System & Frontend Guide

This document defines the **visual identity, design system, and frontend engineering standards** for the Marketplace project. All developers and agents MUST adhere to these guidelines to ensure a premium, enterprise-grade user experience.

---

## 1. Design Philosophy: "Enterprise Gateway"

Our design focuses on **high integrity, trust, and fluid efficiency**. We use the **Liquid Glass** aesthetic to provide a modern, premium feel while maintaining the robustness required for an enterprise B2B/B2C marketplace.

### Core Principles:
- **Premium Aesthetics**: Use subtle gradients, backdrop blurs (glassmorphism), and refined typography to "WOW" the user.
- **High Agency**: Provide clear paths for different user roles (Buyer, Seller, Admin) with prominent trust signals.
- **Micro-interactions**: Every click and hover should feel alive with smooth, purposeful animations.
- **Zero Placeholder Policy**: Use real-looking data and generated assets for all demonstrations.

---

## 2. Visual Language

### 2.1 Color Palette (Corporate Trust)
| Role | Hex | Tailwind Class | Purpose |
|:---:|:---:|:---:|:---|
| **Primary** | `#7C3AED` | `bg-primary` | Brand identity, primary actions (Trust Purple) |
| **Secondary** | `#A78BFA` | `bg-secondary` | Muted actions, accents |
| **Success** | `#22C55E` | `bg-success` | Confirmations, "Buy" buttons (Transaction Green) |
| **Background**| `#FAF5FF` | `bg-background` | Main canvas (Soft Purple tint) |
| **Foreground**| `#4C1D95` | `text-foreground`| Primary text (Deep Purple) |
| **Muted** | `#6B7280` | `text-muted` | Secondary text, descriptions |

### 2.2 Typography
- **Headings**: `Lexend` — Professional, clean, and highly readable for enterprise contexts.
- **Body**: `Source Sans 3` — Optimized for long-form reading and data-heavy interfaces.

**Google Fonts Import:**
```css
@import url('https://fonts.googleapis.com/css2?family=Lexend:wght@300;400;500;600;700&family=Source+Sans+3:wght@300;400;500;600;700&display=swap');
```

### 2.3 Key Visual Effects (Liquid Glass)
- **Glassmorphism**: Use `backdrop-blur-md` and `bg-white/70` (light) or `bg-slate-900/70` (dark) for cards and navbars.
- **Soft Shadows**: Use multi-layered shadows for depth (`shadow-xl` with subtle purple tinting).
- **Smooth Curves**: All interactive elements should use `rounded-xl` or `rounded-2xl`.

---

## 3. Tech Stack & Implementation

- **Framework**: Angular 21+ (Standalone, Signals, Zoneless ready)
- **CSS**: Tailwind CSS v4 (using the `@theme` directive)
- **UI Components**: `@spartan-ng` (Headless primitives for maximum flexibility)
- **State**: NgRx SignalStore (State-of-the-art signal-based state management)
- **Icons**: `Lucide Angular` (Consistent, clean SVG icons)

### 3.1 Theming Strategy
The application supports native Light and Dark modes using the `class` strategy in Tailwind.

- **Light Mode**: High contrast, crisp borders (`border-border`), and purple-tinted backgrounds.
- **Dark Mode**: Deep navy/slate palette (`#0F172A`), glowing primary accents, and refined transparency.

### 3.2 Spartan/UI Guidelines
- **Don't reinvent the wheel**: Use Spartan's `brn-` primitives for Dialogs, Popovers, Tabs, and Selects.
- **Customization**: Style Spartan components using the design tokens defined in `tailwind.config.js`.

---

## 4. Interaction & Motion

### 4.1 Animation Specs
- **Hover States**: 200ms `ease-in-out`. Use subtle scale (`scale-[1.02]`) and shadow enhancement.
- **Page Transitions**: 400ms `cubic-bezier(0.4, 0, 0.2, 1)`.
- **Loading States**: Use refined skeletons that mirror the final component structure.

### 4.2 Interaction Rules
- **Cursor**: Always use `cursor-pointer` on all clickable cards and buttons.
- **Feedback**: Provide immediate visual feedback on all interactions (ripple effects or color shifts).
- **Accessibility**: Ensure `focus-visible` rings are prominent for keyboard navigation.

---

## 5. Internationalization (i18n)

We use Angular's native `@angular/localize` system.

- **No Hardcoded Strings**: All text must be wrapped in `i18n` attributes or `$localize`.
- **Dynamic Data**: Use Angular pipes (`currency`, `date`, `number`) for locale-aware formatting.
- **RTL Support**: Design layouts to be direction-agnostic where possible.

---

## 6. Pre-Delivery Checklist (QA)
- [ ] **Accessibility**: Contrast ratio >= 4.5:1 for all text.
- [ ] **Icons**: No emojis. Only Lucide SVG icons.
- [ ] **Performance**: Images optimized/lazy-loaded. Components use `OnPush`.
- [ ] **Responsive**: Verified at 375px (Mobile), 768px (Tablet), 1440px (Desktop).
- [ ] **Motion**: `prefers-reduced-motion` media query respected.

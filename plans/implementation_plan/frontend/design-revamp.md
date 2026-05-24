# Marketplace UI Revamp: Practical & Clean Design

> **For Hermes:** Use subagent-driven-development skill to implement this plan task-by-task.

**Goal:** Remove all glassy/semi-transparent design patterns and replace with solid, practical, user-friendly UI. Maintain purple brand identity but make it more professional.

**Architecture:** CSS-only changes (theme + component templates). No logic changes. All tests should pass after each task since we're only modifying visual markup.

**Tech Stack:** Angular 21, Tailwind CSS v4, Lucide Icons, Spartan UI

---

## Design System Changes

### What We're Removing
- `backdrop-blur-*` everywhere
- `bg-card/40`, `bg-card/50`, `bg-card/90` (semi-transparent cards)
- `bg-background/80` (semi-transparent header)
- `bg-muted/20`, `bg-primary/10` (heavy opacity usage)
- `shadow-primary/20` (colored shadows)
- `rounded-3xl` (over-rounded)
- `bg-background/80 backdrop-blur-md` (blurry badges)

### What We're Adding
- Solid `bg-card` backgrounds with subtle `border-border` borders
- Clean `shadow-sm` or no shadow on cards
- Solid `bg-primary` buttons without colored glows
- `rounded-xl` (consistent, moderate rounding)
- Better text contrast in light mode
- Professional, flat feel

### Color Theme Update
- Keep purple primary (#7C3AED light, #8B5CF6 dark)
- Cleaner background: `#ffffff` (light), `#0f172a` (dark)
- Better text contrast: `#0f172a` (light), `#f8fafc` (dark)
- Cards: solid white / slate-800

---

## Task 1: Update Global Theme (styles.css)

**Objective:** Fix the base theme to use solid colors with better contrast.

**Files:**
- Modify: `src/web/src/styles.css`

**Changes:**
```css
@layer base {
  :root {
    --background: #ffffff;
    --foreground: #0f172a;
    --primary: #7c3aed;
    --secondary: #6d28d9;
    --success: #10b981;
    --muted: #64748b;
    --muted-foreground: #475569;
    --border: #e2e8f0;
    --card: #ffffff;
    --card-foreground: #0f172a;
    --input: #ffffff;
  }

  .dark {
    --background: #0f172a;
    --foreground: #f8fafc;
    --primary: #8b5cf6;
    --secondary: #7c3aed;
    --muted: #94a3b8;
    --muted-foreground: #cbd5e1;
    --border: #1e293b;
    --card: #1e293b;
    --card-foreground: #f8fafc;
    --input: #1e293b;
  }
}
```

**Verify:** `npx ng test --watch=false` — all tests pass.

---

## Task 2: Remove Glassy Header

**Objective:** Make header solid with clean background.

**Files:**
- Modify: `src/web/src/app/shared/components/header/header.html`

**Changes:**
- Line 2: `bg-background/80 backdrop-blur-xl` → `bg-card border-b border-border shadow-sm`
- Line 14: Remove `shadow-lg shadow-primary/20` from logo icon
- Line 23: Remove `shadow-lg shadow-primary/20` from Catalog button
- Line 75: `bg-card/50` → `bg-card` on user menu trigger
- Line 94: `bg-card/90 backdrop-blur-xl` → `bg-card shadow-lg` on dropdown

**Verify:** Visual check — header is solid, no transparency.

---

## Task 3: Fix Product Cards

**Objective:** Solid product cards without glass effect.

**Files:**
- Modify: `src/web/src/app/features/catalog/components/product-card/product-card.ts`

**Changes:**
- Line 15-17: `bg-card/40 backdrop-blur-sm` → `bg-card` and `shadow-sm hover:shadow-md`
- Line 45-46: Badge `bg-background/80 backdrop-blur-md` → `bg-card border border-border shadow-sm`
- Line 54-55: Stock badge `bg-green-500/10` → `bg-emerald-50 text-emerald-700 border-emerald-200` (dark: `dark:bg-emerald-950 dark:text-emerald-300 dark:border-emerald-800`)
- Line 92: Remove `shadow-md shadow-primary/20` from add-to-cart button

**Verify:** Visual check — cards are solid white with clean borders.

---

## Task 4: Fix Product Detail Page

**Objective:** Remove glass effects from product detail.

**Files:**
- Modify: `src/web/src/app/features/catalog/product-detail/product-detail.ts`

**Changes:**
- Line 72: `bg-card/40 backdrop-blur-sm` → `bg-card` on image container
- Line 89: `bg-primary/10 text-primary` → `bg-violet-50 text-violet-700 border border-violet-200` (category badge)
- Line 121: `bg-muted/20 border border-border/50` → `bg-muted/10 border border-border` (tags)
- Line 176: `bg-muted/10` → `bg-card border border-border` (sort select)

**Verify:** Visual check — product page is clean and solid.

---

## Task 5: Fix Cart Drawer

**Objective:** Solid cart drawer without glass.

**Files:**
- Modify: `src/web/src/app/shared/components/cart-drawer/cart-drawer.html`

**Changes:**
- Remove any `backdrop-blur` classes
- Ensure cart drawer uses solid `bg-card` background
- Clean up any semi-transparent overlays

**Verify:** Visual check — cart drawer is solid.

---

## Task 6: Fix Cart Page

**Objective:** Clean cart page without glass.

**Files:**
- Modify: `src/web/src/app/features/cart/cart-page/cart-page.ts`

**Changes:**
- Line 29: `bg-card/60 backdrop-blur-sm` → `bg-card` on empty state
- Line 45: `bg-card/60 backdrop-blur-sm` → `bg-card` on cart items container

**Verify:** Visual check — cart page is solid.

---

## Task 7: Fix Checkout Pages

**Objective:** Clean checkout without glass.

**Files:**
- Modify: `src/web/src/app/features/checkout/checkout-summary/checkout-summary.ts`
- Modify: `src/web/src/app/features/checkout/checkout-page/checkout-page.html`
- Modify: `src/web/src/app/features/checkout/address-form/address-form.html`

**Changes:**
- Remove `bg-card/60 backdrop-blur-sm` patterns
- Use solid `bg-card` with `border border-border rounded-xl`

**Verify:** Visual check — checkout is clean.

---

## Task 8: Fix Seller Dashboard & Admin Pages

**Objective:** Clean dashboard pages.

**Files:**
- Modify: `src/web/src/app/features/seller-dashboard/dashboard-page/dashboard-page.ts`
- Modify: `src/web/src/app/features/seller-dashboard/seller-orders/seller-orders.ts`
- Modify: `src/web/src/app/features/seller-dashboard/inventory-list/inventory-list.ts`
- Modify: `src/web/src/app/features/seller-dashboard/store-settings/store-settings.ts`
- Modify: `src/web/src/app/features/admin/store-verification/store-verification.ts`

**Changes:**
- Replace all `bg-card/*` opacity patterns with solid `bg-card`
- Remove all `backdrop-blur-*`
- Clean up colored shadows

**Verify:** `npx ng test --watch=false` — all tests pass.

---

## Task 9: Fix Order Pages

**Objective:** Clean order pages.

**Files:**
- Modify: `src/web/src/app/features/orders/order-detail/order-detail.ts`

**Changes:**
- Replace glass patterns with solid backgrounds
- Clean up order timeline and status badges

**Verify:** Visual check — order pages are clean.

---

## Task 10: Fix Remaining Components

**Objective:** Clean up any remaining glass patterns across the app.

**Files:**
- Modify: `src/web/src/app/features/catalog/components/buy-box/buy-box.ts`
- Modify: `src/web/src/app/features/catalog/components/review-summary/review-summary.ts`
- Modify: `src/web/src/app/features/catalog/components/frequently-bought-together/frequently-bought-together.ts`
- Modify: `src/web/src/app/shared/components/mega-menu/mega-menu.html`
- Modify: `src/web/src/app/features/home/components/hero-banner/hero-banner.ts`
- Modify: `src/web/src/app/features/home/components/product-carousel/product-carousel.ts`
- Modify: `src/web/src/app/features/home/components/deal-of-the-day/deal-of-the-day.ts`

**Changes:**
- Search and replace all remaining `backdrop-blur` patterns
- Replace `bg-card/*` with `bg-card`
- Replace `bg-background/*` with `bg-card` or `bg-background`
- Remove colored shadows

**Verify:** `npx ng test --watch=false` — all tests pass.

---

## Task 11: Final Verification

**Objective:** Ensure everything works.

**Steps:**
1. Run `npx ng test --watch=false` — all 293+ tests pass
2. Run `npx ng build` — build succeeds
3. Visual review of key pages (home, catalog, product detail, cart, checkout)

---

## Acceptance Criteria

- [ ] No `backdrop-blur-*` classes anywhere in component templates
- [ ] No `bg-card/*` opacity patterns (e.g., `bg-card/40`, `bg-card/60`)
- [ ] No `bg-background/80` or similar semi-transparent backgrounds
- [ ] No `shadow-primary/*` colored shadows
- [ ] All cards use solid `bg-card` with `border-border`
- [ ] Header uses solid background (not transparent/blurry)
- [ ] Light mode text contrast ≥ 4.5:1
- [ ] All 293+ tests pass
- [ ] Build succeeds
- [ ] Design looks clean, professional, and practical

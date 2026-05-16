# Next Steps — Implementation Plans

**Created:** 2026-05-16
**Purpose:** Independent, parallelizable implementation plans for marketplace features
**Based on:** `plans/future_design/` design documents + `plans/MISSING.md` gaps

---

## Plans

| # | Plan | Scope | Dependencies | Priority |
|---|------|-------|--------------|----------|
| 01 | [Global Header & Mega-Menu](01-global-header-mega-menu.md) | Frontend + Catalog.API tree endpoint | None | P1 ✅ |
| 02 | [User Profile Hub](02-user-profile-hub.md) | Identity.API + Frontend profile | None | P1 ✅ verified |
| 03 | [Cart & Checkout Optimization](03-cart-checkout-optimization.md) | Cart.API + Ordering.API + Frontend | None | P1 ✅ |
| 04 | [Product Detail Enhancements](04-product-detail-enhancements.md) | Catalog.API + Inventory + Frontend | None | P1 ✅ |
| 05 | [Reviews & Ratings](05-reviews-ratings.md) | Catalog.API + Media.API + Frontend | None | P2 ✅ |
| 06 | [Homepage Content Blocks](06-homepage-content-blocks.md) | Catalog.API + Frontend | Plan 01 (category tree) | P2 ✅ |
| 07 | [Search & Discovery](07-search-discovery.md) | Search.API + Identity + Frontend | None | P2 |
| 08 | [Inventory Management UI](08-inventory-management-ui.md) | Inventory.API + Frontend | None | P1 |
| 09 | [Order Cancellation & Status](09-order-cancellation.md) | Ordering.API + Notification + Frontend | None | P1 |

---

## Execution Strategy

### Parallel Group 1 (No dependencies)
These can all run simultaneously:
- **Plan 01** — Global Header & Mega-Menu
- **Plan 02** — User Profile Hub
- **Plan 03** — Cart & Checkout Optimization
- **Plan 04** — Product Detail Enhancements
- **Plan 05** — Reviews & Ratings
- **Plan 07** — Search & Discovery
- **Plan 08** — Inventory Management UI
- **Plan 09** — Order Cancellation & Status

### Parallel Group 2 (After Group 1)
- **Plan 06** — Homepage Content Blocks (~~depends on Plan 01~~ Plan 01 done — can run now)

---

## Agent Instructions

Each plan file is self-contained with:
1. **Goal** — What to build
2. **Context** — Current state, target state, design references
3. **Prerequisites** — What already exists
4. **Backend Changes** — Step-by-step with code snippets
5. **Frontend Changes** — Step-by-step with code snippets
6. **Files to Modify/Create** — Complete list
7. **Verification** — How to test

### Running a plan:
1. Read the plan file completely
2. Follow the steps in order
3. Create files as specified
4. Run verification steps
5. Report completion with test results

### Important:
- Each plan assumes other plans are NOT yet implemented
- Plans are designed to be mergeable (no conflicting file changes)
- If a plan references a file that another plan also modifies, handle the merge carefully
- All plans follow AGENTS.md conventions (Clean Architecture, CQRS, NgRx SignalStore)

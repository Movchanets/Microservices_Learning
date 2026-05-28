# Task Plan: Rozetka Product Scraper for Seeder.App

## Goal
Create a standalone Node.js Playwright script (TypeScript) that scrapes real products from Rozetka.com.ua e-commerce site, downloads images locally, and generates a products.json file compatible with the .NET Seeder.App.

## Current Phase
Phase 5

## Phases

### Phase 1: Requirements & Discovery
- [x] Analyze existing project structure
- [x] Understand Seeder.App data format (ProductModel)
- [x] Identify target directory structure (src/Tools/Seeder.App/Data/)
- [x] Document findings in findings.md
- **Status:** complete

### Phase 2: Planning & Structure
- [x] Design scraper architecture
- [x] Define output JSON format matching ProductModel
- [x] Plan anti-bot evasion strategies
- [x] Plan idempotency mechanism
- **Status:** complete

### Phase 3: Implementation
- [x] Create TypeScript scraper script in tests/E2ETests/
- [x] Create package.json with dependencies
- [x] Implement Rozetka page scraping logic
- [x] Implement image downloading
- [x] Implement JSON output generation
- **Status:** complete

### Phase 4: Testing & Verification
- [ ] Test scraper runs without errors
- [ ] Verify JSON output format matches ProductModel
- [ ] Verify images download correctly
- [ ] Test idempotency (skip existing products)
- **Status:** pending

### Phase 5: Documentation & Delivery
- [x] Create plans/ folder with documentation
- [x] Provide CLI instructions
- [x] Document usage and configuration
- **Status:** complete

## Key Questions
1. What Rozetka category URL to scrape? (Laptops/Smartphones) - **Answered**: Configurable via CLI
2. How many products to scrape per run? (Configurable, default 20) - **Answered**: CLI --limit option
3. Should we scrape product detail pages or just listing pages? (Listing + detail for images) - **Answered**: Both for complete data

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Use Playwright for scraping | Handles JavaScript-rendered content, anti-bot measures |
| Store images in Data/Images/{sku}/ | Matches Seeder.App expectations for Media API upload |
| Use fs-extra for file operations | Reliable file system operations with promises |
| Implement random delays | Human-like behavior to avoid detection |
| Check existing products before scraping | Idempotency for interrupted scrapes |
| Use commander for CLI | Professional CLI with help text and validation |
| Use tsx for execution | No compilation step, fast startup |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| None yet | - | - |

## Notes
- Rozetka uses dynamic rendering - Playwright is essential
- Product images need to be downloaded locally for Media API
- JSON format must match ProductModel: StoreName, CategoryName, Name, Description, Price, Currency, Sku, Tags, ImageUrl, InitialStock
- ImageUrl in JSON should be local relative path after download

## Files Created

| File | Purpose |
|------|---------|
| `tests/E2ETests/scripts/rozetka-scraper.ts` | Main scraper script |
| `tests/E2ETests/scripts/package.json` | Node.js dependencies |
| `tests/E2ETests/scripts/tsconfig.json` | TypeScript configuration |
| `plans/rozetka-scraper/README.md` | Implementation plan |
| `plans/rozetka-scraper/QUICKSTART.md` | Quick start guide |
| `plans/rozetka-scraper/TECHNICAL.md` | Technical details |
| `task_plan.md` | This planning document |
| `findings.md` | Research findings |
| `progress.md` | Progress log |

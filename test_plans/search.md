# Test Plan: Search Service

## Current Coverage

| Layer | Test Files | Test Count | Status |
|-------|-----------|------------|--------|
| Unit | ProductCreatedConsumerTests, ProductDeletedConsumerTests, ProductUpdatedConsumerTests | ~12 | Covered |
| Integration | SearchQueryTests, IndexingTests | ~8 | Partially Covered |
| Contract | CatalogToSearchContractTests | ~5 | Covered |
| E2E | — | 0 | Not Covered |

## Test Scenarios — Unit

- [x] ProductCreatedConsumer indexes document
- [x] ProductUpdatedConsumer updates document
- [x] ProductDeletedConsumer removes document
- [ ] Consumer error handling (ES unavailable)
- [ ] Consumer with malformed event data
- [ ] Bulk indexing processor (after optimization)

## Test Scenarios — Integration

- [x] Search query returns matching products
- [x] Indexing creates searchable document
- [ ] Filter by category, price range, brand
- [ ] Full-text search with fuzziness
- [ ] Faceted search (categories, brands, price_ranges)
- [ ] Multi-field search relevance (name^3, description, tags^2)
- [ ] Empty search returns all active products
- [ ] Search with inactive products excluded

## Test Scenarios — E2E

- [ ] Search from header input
- [ ] Search results page with filters
- [ ] Search facets click → results update
- [ ] Search pagination
- [ ] Empty search results state
- [ ] Search with special characters

## Gaps & Priority

| Gap | Priority | Notes |
|-----|----------|-------|
| ES optimization tests | P0 | After implementing explicit mapping + filter context, add tests |
| Bulk indexing tests | P1 | After implementing BulkIndexProcessor |
| Search E2E | P2 | Covered by catalog E2E partially |

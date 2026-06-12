# Marketplace Catalog

The catalog context owns the definition and sellable variants of products. It spans the Catalog, Media, and Search microservices.

## Language

**Product**:
The definition of what something is — name, description, brand, category. Not sellable on its own; only through its SKUs.
_Avoid_: Item, listing

**SKU**:
A sellable variant of a Product. Carries its own price, stock, and typed attributes (color, storage, RAM). Child entity within the Product aggregate.
_Avoid_: Variant, product variant, item

**Variant Axis**:
An AttributeDefinition designated as a dimension along which SKUs differ (e.g., color, storage, RAM). Each Product declares its own set of variant axes. Not all attributes are axes.
_Avoid_: Axis, dimension

**Attribute Definition**:
A category-scoped schema declaration for an attribute (key, display name, value type, allowed values). Defines what attributes a product or SKU *may* have.
_Avoid_: Attribute schema, attribute template

**Typed Attributes**:
Key-value pairs on a SKU that are filterable (GIN-indexed in jsonb). Keys must match an AttributeDefinition for the product's category.
_Avoid_: Filterable attributes, structured attributes

**Flexible Attributes**:
Key-value pairs on a SKU that are freeform and not filterable (no index). Used for non-searchable specs like care instructions or weight.
_Avoid_: Freeform attributes, unstructured attributes

**Gallery Entry**:
A link between a MediaItem and a target entity (Product or SKU). Carries sort order and primary flag. TargetType is always UPPERCASE (`PRODUCT` or `SKU`).
_Avoid_: Image entry, photo link

**Media Item**:
A file stored in blob storage. Linked to targets via GalleryEntry. One MediaItem can be linked to multiple targets.
_Avoid_: File, upload

**Seeder Pipeline**:
The 8-step data import pipeline in `Seeder.App` that reads `catalog.json` and populates the database.
_Avoid_: Importer, data loader

**Scraper**:
The Playwright-based tool in `src/Tools/rozetka-scraper/` that extracts product data from Rozetka into `catalog.json`.
_Avoid_: Crawler, parser

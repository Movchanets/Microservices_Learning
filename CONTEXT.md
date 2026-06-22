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

## Ordering

**Order**:
Aggregate root representing a buyer's purchase intent. Contains buyer identity, shipping address, line items, and a computed total. Tracks its own lifecycle through order statuses. Created via a factory method.
_Avoid_: Purchase, transaction, checkout record

**OrderItem**:
Child entity of Order. Links the order to a specific product variant (SKU) with a quantity and the unit price at time of purchase. Cannot exist independently of its Order.
_Avoid_: Line item, cart item, product line

**OrderStatus**:
Enumeration defining the order lifecycle. Saga path: Submitted → InventoryReserved → PaymentProcessing → Completed. Fulfillment path: Processing → Shipped → Delivered. Terminal states: Cancelled, Faulted.
_Avoid_: State, stage, phase

**FastForwardTo**:
Method on Order that handles race conditions when saga projection events arrive out of order. Sequentially advances through intermediate states to reach the target status rather than requiring strict ordering.
_Avoid_: AdvanceTo, skipTo, jumpTo

**Address**:
Value object for shipping and billing locations. Captures street, city, state, country, and zip code. Two addresses with identical values are considered equal.
_Avoid_: Location, residence, dwelling

## Payment

**PaymentTransaction**:
Aggregate root recording a payment attempt for an order. Tracks amount, status, and the external gateway's transaction identifier. Created via a factory method when an order is ready for payment.
_Avoid_: Charge, billing, payment record

**PaymentStatus**:
Enumeration defining the payment lifecycle. Values: Pending, Completed, Failed, Refunded. Transitions are driven by the external payment gateway response.
_Avoid_: State, stage, phase

**Refund**:
Child entity of PaymentTransaction. Records a refund for a completed payment. Tracks amount, reason, and processing status. Cannot exist independently of its parent transaction.
_Avoid_: Reversal, chargeback, credit

**TransactionId**:
The identifier returned by the external payment gateway when a payment completes successfully. Stored on PaymentTransaction to correlate with gateway records.
_Avoid_: Gateway ID, charge ID, payment reference

## Identity

**User**:
The aggregate root for the Identity domain. Represents a person who can buy, sell, or administer the platform. Created via factory method `User.Create()`. Holds email, password hash, name, role, optional store association, and refresh token state.
_Avoid_: Account, credentials, auth record

**UserRole**:
Bitwise flags enum defining the roles a user can hold — Buyer, Seller, Admin. A user may hold multiple roles simultaneously (e.g., Buyer + Seller).
_Avoid_: Permission, access level

**Email**:
Value object wrapping a validated, normalized email address. Created via `Email.Create()`. Ensures format validity and stores the value in lowercase.
_Avoid_: Email address (raw string), login

**PasswordHash**:
Value object wrapping a BCrypt-hashed password. Created via `PasswordHash.Create()`. Never stores plaintext.
_Avoid_: Password, secret, credentials

**RefreshToken**:
Value object representing an opaque token used for issuing new access tokens. Carries an expiration timestamp. Attached to User as `CurrentRefreshToken`; can be revoked.
_Avoid_: Session token, auth token, JWT

## Cart

**ShoppingCart**:
Aggregate root representing a buyer's active cart session. Holds a collection of CartItems and tracks creation/update timestamps. Backed by Redis as a thin service — no EF Core for cart data. Supports anonymous carts that can be claimed by an authenticated user.
_Avoid_: Basket, bag, order draft

**CartItem**:
Child entity of ShoppingCart. References a specific SKU from a product and tracks quantity and price at time of addition. Identified by the combination of ProductId and SkuId — adding the same product+SKU again increments quantity rather than creating a duplicate.
_Avoid_: Line item, cart entry, product line

**BuyerId**:
Nullable identifier on ShoppingCart linking the cart to an authenticated user. Null for anonymous carts; set when a user logs in or claims the cart. Carts with a BuyerId are scoped to that user's session.
_Avoid_: OwnerId, userId, sessionId

## Inventory

**InventoryItem**:
Aggregate root that tracks stock for a specific SKU. Holds the SKU reference, store association, and quantity state. Created when a SKU is added in Catalog via integration event.
_Avoid_: Stock record, warehouse item, supply entry

**AvailableQuantity**:
The amount of stock available for purchase. Decremented when stock is reserved for an order, incremented when reservations are released or new stock is added.
_Avoid_: On-hand quantity, free stock

**ReservedQuantity**:
The amount of stock held for in-flight orders. Incremented when stock is reserved, decremented when reservations are released. Represents stock that is allocated but not yet shipped.
_Avoid_: Held stock, pending quantity

**Version**:
Concurrency token that prevents lost updates when multiple sellers modify stock simultaneously. Changes automatically on each update.
_Avoid_: Revision, lock, ETag

## StoreManagement

**Store**:
Aggregate root representing a seller's storefront on the marketplace. Holds store metadata (name, description, logo) and tracks its verification lifecycle. Created via `Store.Create()`. Sellers must have a Verified store before listing products.
_Avoid_: Shop, vendor, merchant

**VerificationStatus**:
Enumeration defining the store verification lifecycle. Values: Pending, Verified, Rejected. New stores start as Pending. Admins verify or reject. Only Verified stores can list products.
_Avoid_: State, stage, approval status

**SellerId**:
Identifier linking a Store to its owner User. Set during store creation and immutable after. Used to enforce that only the store owner can manage it.
_Avoid_: OwnerId, userId, merchantId

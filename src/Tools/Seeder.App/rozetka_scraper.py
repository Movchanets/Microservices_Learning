#!/usr/bin/env python3
"""
Rozetka Product Scraper
Scrapes product data + variants + images from Rozetka.com.ua
Generates products.json entries for the Marketplace seeder.

Usage:
    python rozetka_scraper.py <url> [--output products.json] [--images-dir Data/Images]

Examples:
    python rozetka_scraper.py https://rozetka.com.ua/ua/acer-nhdaaeu001/p528975609/
    python rozetka_scraper.py https://rozetka.com.ua/ua/mfyt4afa/p543553245/ --store "Tech Store" --category "Electronics"
"""

import argparse
import json
import os
import re
import sys
import time
from pathlib import Path
from urllib.parse import urljoin, urlparse

import requests
from bs4 import BeautifulSoup

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept-Language": "uk-UA,uk;q=0.9,en;q=0.8",
}

# Rozetka image size suffixes: base_action=large, big=medium, medium=small
IMAGE_SIZE_REPLACEMENTS = {
    "base_action": "base_action",  # keep original large size
}


def slugify(text: str, max_len: int = 60) -> str:
    """Create a filesystem-safe slug from text, handling Cyrillic."""
    text = text.lower().strip()
    # Transliterate common Cyrillic chars
    cyr_map = {
        'а': 'a', 'б': 'b', 'в': 'v', 'г': 'h', 'ґ': 'g', 'д': 'd', 'е': 'e',
        'є': 'ye', 'ж': 'zh', 'з': 'z', 'и': 'y', 'і': 'i', 'ї': 'yi', 'й': 'y',
        'к': 'k', 'л': 'l', 'м': 'm', 'н': 'n', 'о': 'o', 'п': 'p', 'р': 'r',
        'с': 's', 'т': 't', 'у': 'u', 'ф': 'f', 'х': 'kh', 'ц': 'ts', 'ч': 'ch',
        'ш': 'sh', 'щ': 'shch', 'ь': '', 'ю': 'yu', 'я': 'ya',
    }
    result = []
    for ch in text:
        if ch in cyr_map:
            result.append(cyr_map[ch])
        elif ch.isalnum() or ch in '- ':
            result.append(ch)
        # skip other chars
    text = ''.join(result)
    # Replace spaces/underscores with hyphens
    text = re.sub(r"[\s_]+", "-", text)
    # Collapse multiple hyphens
    text = re.sub(r"-+", "-", text)
    return text[:max_len].strip("-")


def fetch_page(url: str) -> BeautifulSoup:
    """Fetch a Rozetka page and return parsed soup."""
    r = requests.get(url, headers=HEADERS, timeout=20, allow_redirects=True)
    r.raise_for_status()
    return BeautifulSoup(r.text, "html.parser")


def extract_jsonld_product(soup: BeautifulSoup) -> dict | None:
    """Extract Product data from JSON-LD structured data."""
    for script in soup.find_all("script", type="application/ld+json"):
        try:
            data = json.loads(script.string)
            if isinstance(data, dict) and data.get("@type") == "Product":
                return data
        except (json.JSONDecodeError, TypeError):
            continue
    return None


def extract_breadcrumbs(soup: BeautifulSoup) -> list[dict]:
    """Extract breadcrumbs from JSON-LD BreadcrumbList."""
    for script in soup.find_all("script", type="application/ld+json"):
        try:
            data = json.loads(script.string)
            if isinstance(data, dict) and data.get("@type") == "BreadcrumbList":
                crumbs = []
                for item in data.get("itemListElement", []):
                    name = item.get("name", "")
                    if not name:
                        item_obj = item.get("item", {})
                        name = item_obj.get("name", "")
                    if name:
                        crumbs.append({"name": name, "position": item.get("position", 0)})
                return crumbs
        except (json.JSONDecodeError, TypeError):
            continue
    return []


def extract_variant_links(soup: BeautifulSoup) -> list[dict]:
    """Extract variant product links from the page."""
    variants = []
    seen_urls = set()

    # Rozetka variant links typically contain /p{digits}/ in the URL
    for a in soup.find_all("a", href=True):
        href = a["href"]
        # Match Rozetka product URLs
        if re.search(r"/p\d+/", href) and href not in seen_urls:
            text = a.get_text(strip=True)
            # Filter out non-variant links (navigation, reviews, etc.)
            skip_words = ["відгук", "comment", "про товар", "читати", "дивитися", "всі"]
            if text and not any(w in text.lower() for w in skip_words):
                # Only include if text looks like a variant name (short)
                if len(text) < 80 and len(text) > 1:
                    full_url = href if href.startswith("http") else urljoin("https://rozetka.com.ua", href)
                    if full_url not in seen_urls:
                        seen_urls.add(full_url)
                        variants.append({"name": text, "url": full_url})

    return variants


def extract_rozetka_id(url: str) -> str | None:
    """Extract Rozetka product ID from URL."""
    m = re.search(r"/p(\d+)/", url)
    return m.group(1) if m else None


def download_images(image_urls: list[str], output_dir: Path, prefix: str = "image") -> list[str]:
    """Download images to output directory. Returns list of local relative paths."""
    output_dir.mkdir(parents=True, exist_ok=True)
    local_paths = []

    for i, url in enumerate(image_urls):
        filename = f"{prefix}{i}.jpg"
        filepath = output_dir / filename

        if filepath.exists():
            print(f"  [skip] {filename} already exists")
            local_paths.append(str(filepath))
            continue

        try:
            r = requests.get(url, headers=HEADERS, timeout=30)
            r.raise_for_status()
            filepath.write_bytes(r.content)
            local_paths.append(str(filepath))
            print(f"  [ok] {filename} ({len(r.content)} bytes)")
            time.sleep(0.3)  # polite delay
        except Exception as e:
            print(f"  [err] {filename}: {e}")

    return local_paths


def scrape_product(url: str) -> dict:
    """Scrape a single Rozetka product page."""
    print(f"\nScraping: {url}")
    soup = fetch_page(url)

    product_data = extract_jsonld_product(soup)
    if not product_data:
        raise ValueError(f"No Product JSON-LD found at {url}")

    rozetka_id = extract_rozetka_id(url) or product_data.get("sku", "")
    name = product_data.get("name", "")
    description = product_data.get("description", "")
    images = product_data.get("image", [])
    if isinstance(images, str):
        images = [images]

    price_data = product_data.get("offers", {})
    price = price_data.get("price", 0)
    currency = price_data.get("priceCurrency", "UAH")

    breadcrumbs = extract_breadcrumbs(soup)
    category_path = " > ".join(b["name"] for b in breadcrumbs if b.get("name"))

    # Extract variants
    variant_links = extract_variant_links(soup)
    print(f"  Found {len(variant_links)} variant links")

    return {
        "rozetka_id": rozetka_id,
        "name": name,
        "description": description,
        "price": price,
        "currency": currency,
        "images": images,
        "breadcrumbs": breadcrumbs,
        "category_path": category_path,
        "variant_links": variant_links,
        "url": url,
    }


def scrape_full_product(url: str, images_base_dir: Path, store_name: str = "Tech Store", category_override: str = "") -> dict:
    """Scrape product + all variants, download images, return products.json entry."""
    # Scrape main product
    main = scrape_product(url)
    rozetka_id = main["rozetka_id"]
    slug = slugify(main["name"])

    print(f"\nProduct: {main['name'][:80]}")
    print(f"  Rozetka ID: {rozetka_id}")
    print(f"  Price: {main['price']} {main['currency']}")
    print(f"  Images: {len(main['images'])}")

    # Download main product images
    main_img_dir = images_base_dir / slug
    print(f"\nDownloading images to {main_img_dir}...")
    download_images(main["images"], main_img_dir)

    # Build category from breadcrumbs
    category = category_override
    if not category and main["breadcrumbs"]:
        # Use last breadcrumb as category (most specific)
        category_names = [b["name"] for b in main["breadcrumbs"]]
        # Skip first (Rozetka) and last (product name)
        if len(category_names) > 2:
            category = " > ".join(category_names[1:])
        elif len(category_names) > 1:
            category = category_names[-2]

    # Scrape variants
    variants = []
    for vlink in main["variant_links"]:
        v_url = vlink["url"]
        v_id = extract_rozetka_id(v_url)
        if not v_id or v_id == rozetka_id:
            continue  # skip self-link

        try:
            print(f"\n  Scraping variant: {vlink['name']}")
            v_data = scrape_product(v_url)
            v_slug = slugify(v_data["name"])
            v_img_dir = images_base_dir / v_slug
            print(f"  Downloading {len(v_data['images'])} variant images...")
            download_images(v_data["images"], v_img_dir)

            # Build relative image paths for products.json
            gallery = [f"Images/{v_slug}/image{i}.jpg" for i in range(len(v_data["images"]))]

            variants.append({
                "RozetkaCode": v_id,
                "Name": vlink["name"],
                "Type": "storage" if any(kw in vlink["name"].lower() for kw in ["гб", "тб", "gb", "tb"]) else "model",
                "Price": v_data["price"],
                "ImageUrl": gallery[0] if gallery else "",
                "Gallery": gallery,
            })

            time.sleep(0.5)  # polite delay between variants
        except Exception as e:
            print(f"  [err] Variant {vlink['name']}: {e}")

    # Build main product gallery paths
    main_gallery = [f"Images/{slug}/image{i}.jpg" for i in range(len(main["images"]))]

    # Build tags from breadcrumbs
    tags = []
    for b in main["breadcrumbs"]:
        name = b.get("name", "")
        if name and name != "Інтернет-магазин Rozetka" and len(name) > 2:
            tags.append(name.lower())

    # Build final products.json entry
    entry = {
        "StoreName": store_name,
        "CategoryName": category or "Electronics",
        "Name": main["name"],
        "Description": main["description"],
        "Price": main["price"],
        "Currency": main["currency"],
        "Sku": f"ROZ-{rozetka_id}",
        "RozetkaCode": rozetka_id,
        "Tags": tags,
        "ImageUrl": main_gallery[0] if main_gallery else "",
        "Gallery": main_gallery,
        "Breadcrumbs": main["breadcrumbs"],
        "CategoryPath": main["category_path"],
        "InitialStock": 25,
        "Variants": variants,
    }

    return entry


def main():
    parser = argparse.ArgumentParser(description="Scrape Rozetka product pages")
    parser.add_argument("urls", nargs="+", help="Rozetka product URLs to scrape")
    parser.add_argument("--output", "-o", default=None, help="Output JSON file (appends to existing)")
    parser.add_argument("--images-dir", "-i", default="Data/Images", help="Images output directory")
    parser.add_argument("--store", "-s", default="Tech Store", help="Store name for products.json")
    parser.add_argument("--category", "-c", default="", help="Override category name")
    args = parser.parse_args()

    # Resolve paths relative to script location
    script_dir = Path(__file__).parent
    images_dir = script_dir / args.images_dir
    output_file = script_dir / args.output if args.output else None

    results = []
    for url in args.urls:
        try:
            entry = scrape_full_product(url, images_dir, args.store, args.category)
            results.append(entry)
            print(f"\n{'='*60}")
            print(f"Product: {entry['Name'][:80]}")
            print(f"  SKU: {entry['Sku']}")
            print(f"  Images: {len(entry['Gallery'])}")
            print(f"  Variants: {len(entry['Variants'])}")
            for v in entry['Variants']:
                print(f"    - {v['Name']}: ROZ-{v['RozetkaCode']} ({v['Price']} UAH, {len(v['Gallery'])} images)")
        except Exception as e:
            print(f"\n[ERROR] {url}: {e}")

    if output_file and results:
        # Load existing products
        existing = []
        if output_file.exists():
            with open(output_file, "r", encoding="utf-8") as f:
                existing = json.load(f)

        # Merge: update existing or add new
        existing_by_sku = {p["Sku"]: p for p in existing}
        for entry in results:
            existing_by_sku[entry["Sku"]] = entry

        merged = list(existing_by_sku.values())
        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(merged, f, indent=2, ensure_ascii=False)

        print(f"\nWrote {len(merged)} products to {output_file}")
    elif results:
        # Print to stdout
        print(json.dumps(results, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()

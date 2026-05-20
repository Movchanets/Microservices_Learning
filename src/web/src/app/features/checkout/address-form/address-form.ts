import { Component, ChangeDetectionStrategy, OnInit, inject, output, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';

export interface Address {
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

@Component({
  selector: 'app-address-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  templateUrl: './address-form.html',
  styleUrl: './address-form.css',
})
export class AddressFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private platformId = inject(PLATFORM_ID);

  addressSaved = output<Address>();

  addressForm!: FormGroup;

  countries = [
    { code: 'US', name: 'United States' },
    { code: 'CA', name: 'Canada' },
    { code: 'GB', name: 'United Kingdom' },
    { code: 'AU', name: 'Australia' },
    { code: 'DE', name: 'Germany' },
    { code: 'FR', name: 'France' },
    { code: 'UA', name: 'Ukraine' },
  ];

  ngOnInit() {
    let initialAddress: Record<string, unknown> = {};
    if (isPlatformBrowser(this.platformId)) {
      const savedAddress = localStorage.getItem('marketplace_shipping_address');
      initialAddress = savedAddress ? JSON.parse(savedAddress) : {};
    }

    this.addressForm = this.fb.group({
      addressLine1: [initialAddress['addressLine1'] || '', [Validators.required]],
      addressLine2: [initialAddress['addressLine2'] || ''],
      city: [initialAddress['city'] || '', [Validators.required]],
      state: [initialAddress['state'] || '', [Validators.required]],
      postalCode: [initialAddress['postalCode'] || '', [Validators.required, Validators.pattern(/^[0-9A-Z\s-]+$/i)]],
      country: [initialAddress['country'] || 'US', [Validators.required]],
    });
  }

  submit() {
    if (this.addressForm.valid) {
      const address = this.addressForm.value;
      if (isPlatformBrowser(this.platformId)) {
        localStorage.setItem('marketplace_shipping_address', JSON.stringify(address));
      }
      this.addressSaved.emit(address);
    }
  }
}

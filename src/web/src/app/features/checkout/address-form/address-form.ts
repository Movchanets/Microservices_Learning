import { Component, ChangeDetectionStrategy, inject, output, PLATFORM_ID } from '@angular/core';
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
export class AddressFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly platformId = inject(PLATFORM_ID);

  addressSaved = output<Address>();

  countries = [
    { code: 'US', name: 'United States' },
    { code: 'CA', name: 'Canada' },
    { code: 'GB', name: 'United Kingdom' },
    { code: 'AU', name: 'Australia' },
    { code: 'DE', name: 'Germany' },
    { code: 'FR', name: 'France' },
    { code: 'UA', name: 'Ukraine' },
  ];

  private readonly initialAddress = this.loadSavedAddress();

  addressForm: FormGroup = this.fb.group({
    addressLine1: [this.initialAddress['addressLine1'] || '', [Validators.required, Validators.maxLength(250)]],
    addressLine2: [this.initialAddress['addressLine2'] || ''],
    city: [this.initialAddress['city'] || '', [Validators.required, Validators.maxLength(100)]],
    state: [this.initialAddress['state'] || '', [Validators.required, Validators.maxLength(100)]],
    postalCode: [this.initialAddress['postalCode'] || '', [Validators.required, Validators.pattern(/^[0-9A-Z\s-]{3,10}$/i)]],
    country: [this.initialAddress['country'] || 'US', [Validators.required]],
  });

  private loadSavedAddress(): Record<string, unknown> {
    if (isPlatformBrowser(this.platformId)) {
      const saved = localStorage.getItem('marketplace_shipping_address');
      return saved ? JSON.parse(saved) : {};
    }
    return {};
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

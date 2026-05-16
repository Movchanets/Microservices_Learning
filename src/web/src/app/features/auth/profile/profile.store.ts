import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { AuthService } from '../../../core/auth/auth.service';
import { UpdateProfileRequest, ChangePasswordRequest } from '../../../core/auth/auth.models';

interface ProfileState {
  updating: boolean;
  changingPassword: boolean;
  error: string | null;
  successMessage: string | null;
}

const initialState: ProfileState = {
  updating: false,
  changingPassword: false,
  error: null,
  successMessage: null,
};

export const ProfileStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withMethods((store, authService = inject(AuthService)) => ({
    async updateProfile(id: string, request: UpdateProfileRequest) {
      patchState(store, { updating: true, error: null, successMessage: null });
      try {
        await authService.updateProfile(id, request);
        patchState(store, { updating: false, successMessage: 'Profile updated successfully' });
      } catch (err: any) {
        patchState(store, { updating: false, error: err.error?.title || err.message || 'Update failed' });
      }
    },
    async changePassword(request: ChangePasswordRequest) {
      patchState(store, { changingPassword: true, error: null, successMessage: null });
      try {
        await authService.changePassword(request);
        patchState(store, { changingPassword: false, successMessage: 'Password changed successfully' });
      } catch (err: any) {
        patchState(store, { changingPassword: false, error: err.error?.title || err.message || 'Change password failed' });
      }
    },
    clearMessages() {
      patchState(store, { error: null, successMessage: null });
    }
  }))
);
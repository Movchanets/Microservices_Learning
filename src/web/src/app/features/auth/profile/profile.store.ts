import { inject } from '@angular/core';
import { signalStore, withState, withMethods, patchState } from '@ngrx/signals';
import { AuthService } from '../../../core/auth/auth.service';
import { UpdateProfileRequest, ChangePasswordRequest } from '../../../core/auth/auth.models';
import { extractHttpError } from '../../../core/utils/http.utils';

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
      } catch (err: unknown) {
        patchState(store, {
          updating: false,
          error: extractHttpError(err, 'Update failed'),
        });
      }
    },
    async changePassword(request: ChangePasswordRequest) {
      patchState(store, { changingPassword: true, error: null, successMessage: null });
      try {
        await authService.changePassword(request);
        patchState(store, { changingPassword: false, successMessage: 'Password changed successfully' });
      } catch (err: unknown) {
        patchState(store, {
          changingPassword: false,
          error: extractHttpError(err, 'Change password failed'),
        });
      }
    },
    clearMessages() {
      patchState(store, { error: null, successMessage: null });
    }
  }))
);
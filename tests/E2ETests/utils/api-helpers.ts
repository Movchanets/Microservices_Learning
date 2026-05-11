import { request, APIRequestContext } from '@playwright/test';

export class ApiHelpers {
  readonly request: APIRequestContext;

  constructor(request: APIRequestContext) {
    this.request = request;
  }

  async createTestUser(userData: any) {
    // Logic to call BFF/Identity API to create a user
    // return this.request.post('/api/auth/register', { data: userData });
  }

  async deleteTestUser(email: string) {
    // Logic to cleanup test user
  }
}

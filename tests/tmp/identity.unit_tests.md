# Task: Implement Identity Service Unit Tests

**Source Plan**: `implementation_plan/tests/identity.unit_tests.md`

Goal: Implement unit tests for the Identity domain and application layers using xUnit, Moq, and FluentAssertions. Target >80% coverage on core logic.

Context: 
- Location: src/Microservices/Identity/
- Target Project: tests/UnitTests/Identity.UnitTests
- References: Identity.Domain, Identity.Application

Action Items:
1. Project Setup:
   - Verify/Create tests/UnitTests/Identity.UnitTests.
   - Install NuGet packages: xunit, Moq, FluentAssertions.
2. Domain Layer Tests (User Aggregate):
   - Test: User creation fails with empty/null email.
   - Test: User creation generates appropriate domain events.
   - Test: Profile updates (Name, Address) work and preserve identity.
   - Test: Business rules/constraints on Address value object.
3. Application Layer Tests (Commands/Queries):
   - Test: RegisterUserCommandHandler successfully registers user and commits.
   - Test: RegisterUserCommandHandler returns failure if email already exists (mocking repository).
   - Test: LoginCommandHandler returns successful Result and token upon correct credentials.
   - Test: GetUserByIdQueryHandler returns data when user exists and null/failure when not.

Validation:
- Run: dotnet test tests/UnitTests/Identity.UnitTests/Identity.UnitTests.csproj
- Ensure all assertions pass and there are no warnings.

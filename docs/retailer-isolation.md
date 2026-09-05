# Retailer isolation package

ApiDemo consumes `Cqrs.RetailerIsolation.EfCore` version `1.0.0` from its checked-in local feed at `.nuget/local`. NuGet resolves the package's EF Core dependency transitively.

The message API now uses EF Core/SQLite and applies the package at runtime. `/api/messages` requires the `RetailerAccess` policy, which validates JWT authentication and a UUID claim named `retailer_id`.

The signing key is not stored in source control. Configure it in the deployment environment as `Authentication__Jwt__SigningKey`; it must match the trusted JWT issuer's signing key. The application deliberately does not expose a login/token-issuing endpoint, because a caller must not choose their own retailer identity.

For additional retailer-owned EF Core entities:

1. Implement `Cqrs.RetailerIsolation.IBelongsToRetailer` on that entity.
2. Implement `IRetailerContext` using the authenticated retailer identity.
3. Register `RetailerSaveChangesInterceptor` through `DbContextOptionsBuilder.AddInterceptors`.
4. Add a global EF Core query filter scoped to `IRetailerContext.RetailerId`.

Do not derive the retailer identity from an untrusted request header.

# Brand Refactoring Task List

## Naming Convention Changes
- **Business → Brand** (all references)
- **Tenant → Brand** (for multi-tenant context)
- **BusinessOwner → BrandOwner** (role name)
- **biz_ → brand_** (schema prefix)

## ✅ Completed Tasks
1. ✅ Rename Business entity to Brand
2. ✅ Rename Tenant folder to Brand folder
3. ✅ Update all Tenant classes to Brand classes (TenantUser → BrandUser, etc.)
4. ✅ Update BrandDbContext (formerly TenantDbContext)
5. ✅ Update BrandConfiguration (formerly BusinessConfiguration)

## 🚧 In Progress
- Update schema prefix from biz_ to brand_ in all services

## 📋 Pending Tasks

### Service Layer Updates
- [ ] Update all Business references in services to Brand
  - [ ] BusinessService → BrandService
  - [ ] IBusinessService → IBrandService
  - [ ] TenantConnectionService → BrandConnectionService
  - [ ] Update all method names and parameters
  
### GraphQL Module Updates
- [ ] Update all Business references in GraphQL modules
  - [ ] BusinessModule → BrandModule
  - [ ] BusinessQueries → BrandQueries
  - [ ] BusinessMutations → BrandMutations
  - [ ] Update all GraphQL type names

### Type and Input Updates
- [ ] Update Business types to Brand
  - [ ] BusinessPayloads.cs → BrandPayloads.cs
  - [ ] CreateBusinessInput → CreateBrandInput
  - [ ] UpdateBusinessInput → UpdateBrandInput
  - [ ] All other Business-related types

### Role Updates
- [ ] Update role names
  - [ ] BusinessOwner → BrandOwner in all references
  - [ ] Update TenantRoles → BrandRoles class references

### Database and Configuration
- [ ] Update AppDbContext
  - [ ] DbSet<Business> → DbSet<Brand>
  - [ ] Update all references
- [ ] Update migration files (or create new ones)
- [ ] Update all comments and documentation

### Service Organization (Additional Task)
- [ ] Create Services/System folder for system services
- [ ] Create Services/Data folder for data services
- [ ] Move appropriate services to respective folders
  - Data services: UserService, BrandService, etc.
  - System services: LogService, MailerService, etc.

## Files to Update

### Entity Files
- ✅ /Data/Brand.cs (was Business.cs)
- ✅ /Data/Brand/BrandUser.cs (was TenantUser.cs)
- ✅ /Data/Brand/BrandUserRole.cs (was TenantUserRole.cs)
- ✅ /Data/Brand/BrandDbContext.cs (was TenantDbContext.cs)
- ✅ /Data/EntityConfigurations/BrandConfiguration.cs (was BusinessConfiguration.cs)

### Service Files (Pending)
- /Services/BusinessService.cs → BrandService.cs
- /Services/TenantConnectionService.cs → BrandConnectionService.cs
- /Services/UserService.cs (update Business references)
- /Services/PostgreSQLManagementService.cs (update comments)
- /Services/ApiCredentialService.cs (update comments if any)

### Module Files (Pending)
- /Modules/BusinessModule.cs → BrandModule.cs
- /Queries/BusinessQueries.cs → BrandQueries.cs (if separate)
- /Mutations/BusinessMutations.cs → BrandMutations.cs (if separate)

### Type Files (Pending)
- /Types/Outputs/BusinessOutputs.cs → BrandOutputs.cs
- /Types/Outputs/BusinessAuthOutput.cs → BrandAuthOutput.cs
- /Types/Inputs/CreateBusinessInput.cs → CreateBrandInput.cs
- /Types/Inputs/UpdateBusinessInput.cs → UpdateBrandInput.cs
- /Types/Inputs/SignInInput.cs (update BusinessId → BrandId)

### Other Files (Pending)
- /Data/AppDbContext.cs (update Business references)
- /Program/MailServiceConfig.cs (update service registrations)
- All migration files referencing Business/Tenant

## Search and Replace Patterns

### Code References
- `Business` → `Brand` (class names, types)
- `business` → `brand` (variables, parameters)
- `Tenant` → `Brand` (in multi-tenant context)
- `tenant` → `brand` (variables, parameters)
- `biz_` → `brand_` (schema prefix)
- `BusinessOwner` → `BrandOwner` (role name)
- `BusinessId` → `BrandId` (property/parameter names)
- `businessId` → `brandId` (variable names)

### Comments and Documentation
- "business" → "brand" in comments
- "Business" → "Brand" in XML docs
- "tenant" → "brand" in multi-tenant context comments
- "multi-tenant" can stay as is (it's a technical term)

## Notes
- User confirmed database will be reset, so migrations can be recreated
- Keep the multi-tenant architecture concept but use "Brand" terminology
- Maintain all existing functionality, just rename references
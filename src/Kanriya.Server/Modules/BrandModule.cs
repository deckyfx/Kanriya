using Kanriya.Server.Data;
using Kanriya.Server.Data.BrandSchema;
using Kanriya.Server.Services;
using Kanriya.Server.Services.Data;
using Kanriya.Server.Types;
using Kanriya.Server.Types.Inputs;
using Kanriya.Server.Types.Outputs;
using Kanriya.Server.Queries;
using Kanriya.Server.Mutations;
using Kanriya.Server.Constants;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Kanriya.Server.Modules;

/// <summary>
/// GraphQL module for Brand domain
/// Contains all queries and mutations related to multi-tenant brand management
/// </summary>

#region Brand Queries

[ExtendObjectType(typeof(RootQuery))]
public class BrandQueries
{
    /// <summary>
    /// Get all brands for the current user
    /// </summary>
    [Authorize]
    [GraphQLName("myBrands")]
    [GraphQLDescription("Get all brands accessible to the current user")]
    public async Task<IEnumerable<Brand>> GetMyBrands(
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
            return Array.Empty<Brand>();

        return await brandService.GetUserBrandsAsync(
            currentUser.User.Id);
    }

    /// <summary>
    /// Get a specific brand by ID
    /// </summary>
    [Authorize]
    [GraphQLName("brand")]
    [GraphQLDescription("Get a specific brand by ID if the user has access")]
    public async Task<Brand?> GetBrand(
        string brandId,
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
            return null;

        var brand = await brandService.GetBrandAsync(brandId);
        
        // Only return if user owns the brand
        if (brand != null && brand.OwnerId != currentUser.User.Id)
        {
            // Check if user is SuperAdmin
            bool isSuperAdmin = currentUser.User.UserRoles?.Any(r => r.Role == UserRoles.SuperAdmin) ?? false;
            if (!isSuperAdmin)
                return null;
        }

        return brand;
    }

    // Brand users query removed - not part of simplified architecture

    // Brand statistics removed - not part of simplified architecture

    // Subdomain check removed - not part of simplified architecture
    
    /// <summary>
    /// Get brand info from brand's infoes table
    /// Requires brand-context authentication (not principal)
    /// </summary>
    [Authorize]
    [GraphQLName("brandInfo")]
    [GraphQLDescription("Get all key-value pairs from brand's infoes table (requires brand-context token)")]
    public async Task<IEnumerable<BrandInfo>> GetBrandInfo(
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Check if user has brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        var infoList = await brandService.GetBrandInfoAsync(currentUser.BrandId, cancellationToken);
        
        return infoList.Select(info => new BrandInfo
        {
            Key = info.Key,
            Value = info.Value,
            CreatedAt = info.CreatedAt,
            UpdatedAt = info.UpdatedAt
        });
    }
}

#endregion

#region Brand Mutations

[ExtendObjectType(typeof(RootMutation))]
public class BrandMutations
{
    /// <summary>
    /// Create a new brand
    /// </summary>
    [Authorize]
    [GraphQLName("createBrand")]
    [GraphQLDescription("Create a new brand/tenant with API credentials")]
    public async Task<CreateBrandPayload> CreateBrand(
        CreateBrandInput input,
        [Service] IBrandService brandService,
        [Service] ILogger<BrandMutations> logger,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new CreateBrandPayload
            {
                Success = false,
                Message = "User not authenticated"
            };
        }

        logger.LogDebug("CreateBrand called with name: '{Name}' (length: {Length})", 
            input.Name, 
            input.Name?.Length ?? -1);

        try
        {
            var (brand, apiSecret, apiPassword) = await brandService.CreateBrandAsync(
                input.Name,
                currentUser.User.Id);

            return new CreateBrandPayload
            {
                Success = true,
                Message = "Brand created successfully. Save your API credentials - they won't be shown again.",
                Brand = brand,
                ApiSecret = apiSecret,
                ApiPassword = apiPassword
            };
        }
        catch (InvalidOperationException ex)
        {
            logger.LogDebug("CreateBrand validation failed: {Message}", ex.Message);
            return new CreateBrandPayload
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error creating brand");
            return new CreateBrandPayload
            {
                Success = false,
                Message = "Failed to create brand: " + ex.Message
            };
        }
    }

    // Update brand name removed - brand names in registry are immutable
    // To change display name, update "Brand Name" in brand_xxx.infoes table using brand-context token

    // Suspend brand removed - not part of simplified architecture

    // Activate brand removed - not part of simplified architecture

    /// <summary>
    /// Delete a brand (permanent)
    /// </summary>
    [Authorize] // SuperAdmin or brand owner can delete
    [GraphQLName("deleteBrand")]
    [GraphQLDescription("Permanently delete a brand and all its data (SuperAdmin or brand owner only)")]
    public async Task<DeleteBrandPayload> DeleteBrand(
        string brandId,
        string confirmationText,
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        if (currentUser?.User == null)
        {
            return new DeleteBrandPayload
            {
                Success = false,
                Message = "User not authenticated"
            };
        }

        if (confirmationText != $"DELETE {brandId}")
        {
            return new DeleteBrandPayload
            {
                Success = false,
                Message = $"Please confirm by typing 'DELETE {brandId}'"
            };
        }

        try
        {
            // Check if user is SuperAdmin or brand owner
            bool isSuperAdmin = currentUser.User.UserRoles?.Any(r => r.Role == UserRoles.SuperAdmin) ?? false;
            
            // Get the brand to check ownership
            var brand = await brandService.GetBrandAsync(brandId);
            if (brand == null)
            {
                return new DeleteBrandPayload
                {
                    Success = false,
                    Message = "Brand not found"
                };
            }
            
            // Check if user owns this brand or is SuperAdmin
            bool isOwner = brand.OwnerId == currentUser.User.Id;
            
            if (!isSuperAdmin && !isOwner)
            {
                return new DeleteBrandPayload
                {
                    Success = false,
                    Message = "Only the brand owner or SuperAdmin can delete this brand"
                };
            }

            var success = await brandService.DeleteBrandAsync(brandId);

            return new DeleteBrandPayload
            {
                Success = success,
                Message = success 
                    ? "Brand and all associated data permanently deleted" 
                    : "Failed to delete brand"
            };
        }
        catch (Exception ex)
        {
            return new DeleteBrandPayload
            {
                Success = false,
                Message = "Failed to delete brand: " + ex.Message
            };
        }
    }

    // Add user to brand removed - not part of simplified architecture

    // Remove user from brand removed - not part of simplified architecture

    // Switch brand context removed - not part of simplified architecture
    
    /// <summary>
    /// Update brand info in brand's infoes table
    /// Requires brand-context authentication (not principal)
    /// </summary>
    [Authorize]
    [GraphQLName("updateBrandInfo")]
    [GraphQLDescription("Update a key-value pair in brand's infoes table (requires brand-context token)")]
    public async Task<UpdateBrandInfoPayload> UpdateBrandInfo(
        UpdateBrandInfoInput input,
        [Service] IBrandService brandService,
        [Service] ILogger<BrandMutations> logger,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Check if user has brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId))
        {
            return new UpdateBrandInfoPayload
            {
                Success = false,
                Message = "Brand-context authentication required. Please sign in with brand API credentials."
            };
        }
        
        // Check if user has appropriate role (BrandOwner or BrandOperator)
        var userRoles = currentUser.BrandUser?.Roles?.Select(r => r.Role).ToList() ?? new List<string>();
        if (!userRoles.Contains(BrandRoles.BrandOwner) && !userRoles.Contains(BrandRoles.BrandOperator))
        {
            return new UpdateBrandInfoPayload
            {
                Success = false,
                Message = "Insufficient permissions. BrandOwner or BrandOperator role required."
            };
        }
        
        try
        {
            var result = await brandService.UpdateBrandInfoAsync(
                currentUser.BrandId,
                input.Key,
                input.Value,
                cancellationToken);
            
            logger.LogInformation("Brand {BrandId} info update: {Key} = {Value} - {Success}",
                currentUser.BrandId, input.Key, input.Value, result.Success);
            
            return new UpdateBrandInfoPayload
            {
                Success = result.Success,
                Message = result.Message,
                Key = input.Key,
                Value = input.Value
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update brand info for brand {BrandId}", currentUser.BrandId);
            return new UpdateBrandInfoPayload
            {
                Success = false,
                Message = "Failed to update brand info: " + ex.Message
            };
        }
    }
}

#endregion
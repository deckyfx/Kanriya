using Kanriya.Server.Data.BrandSchema;
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

namespace Kanriya.Server.Modules;

/// <summary>
/// GraphQL module for Outlet domain
/// Contains all queries and mutations related to outlet management within brands
/// </summary>

#region Outlet Queries

[ExtendObjectType(typeof(RootQuery))]
public class OutletQueries
{
    /// <summary>
    /// Get all outlets for the current brand
    /// </summary>
    [Authorize]
    [GraphQLName("outlets")]
    [GraphQLDescription("Get all outlets in the current brand")]
    public async Task<IEnumerable<Outlet>> GetOutlets(
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        return await outletService.GetBrandOutletsAsync(currentUser.BrandId, currentUser.BrandSchema);
    }
    
    /// <summary>
    /// Get a specific outlet by ID
    /// </summary>
    [Authorize]
    [GraphQLName("outlet")]
    [GraphQLDescription("Get a specific outlet by ID")]
    public async Task<Outlet?> GetOutlet(
        string id,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        var outlet = await outletService.GetOutletAsync(currentUser.BrandId, currentUser.BrandSchema, id);
        
        // Check if user has access to this outlet
        if (outlet != null)
        {
            var hasAccess = await outletService.UserHasOutletAccessAsync(
                currentUser.BrandId, 
                currentUser.BrandSchema, 
                currentUser.BrandUser.Id, 
                outlet.Id);
                
            if (!hasAccess)
            {
                return null;
            }
        }
        
        return outlet;
    }
    
    /// <summary>
    /// Get outlets accessible to the current user
    /// </summary>
    [Authorize]
    [GraphQLName("myOutlets")]
    [GraphQLDescription("Get outlets accessible to the current user based on permissions")]
    public async Task<IEnumerable<Outlet>> GetMyOutlets(
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        return await outletService.GetUserOutletsAsync(
            currentUser.BrandId, 
            currentUser.BrandSchema, 
            currentUser.BrandUser.Id);
    }
    
    /// <summary>
    /// Get outlets assigned to a specific user
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("userOutletAccess")]
    [GraphQLDescription("Get outlets assigned to a specific user (BrandOwner only)")]
    public async Task<IEnumerable<Outlet>> GetUserOutletAccess(
        string userId,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        return await outletService.GetUserOutletsAsync(
            currentUser.BrandId, 
            currentUser.BrandSchema, 
            userId);
    }
    
    /// <summary>
    /// Get users who have access to a specific outlet
    /// </summary>
    [Authorize]
    [GraphQLName("outletUsers")]
    [GraphQLDescription("Get users who have access to a specific outlet")]
    public async Task<IEnumerable<BrandUser>> GetOutletUsers(
        string outletId,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        // Check if user has access to this outlet first
        var hasAccess = await outletService.UserHasOutletAccessAsync(
            currentUser.BrandId, 
            currentUser.BrandSchema, 
            currentUser.BrandUser.Id, 
            outletId);
            
        if (!hasAccess)
        {
            throw new GraphQLException("You do not have access to this outlet.");
        }
        
        return await outletService.GetOutletUsersAsync(
            currentUser.BrandId, 
            currentUser.BrandSchema, 
            outletId);
    }
}

#endregion

#region Outlet Mutations

[ExtendObjectType(typeof(RootMutation))]
public class OutletMutations
{
    /// <summary>
    /// Create a new outlet
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("createOutlet")]
    [GraphQLDescription("Create a new outlet (BrandOwner only)")]
    public async Task<CreateOutletOutput> CreateOutlet(
        CreateOutletInput input,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var outlet = await outletService.CreateOutletAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                input.Code,
                input.Name,
                input.Address);
                
            return new CreateOutletOutput
            {
                Success = true,
                Message = $"Outlet '{input.Name}' created successfully",
                Outlet = outlet
            };
        }
        catch (Exception ex)
        {
            return new CreateOutletOutput
            {
                Success = false,
                Message = $"Failed to create outlet: {ex.Message}",
                Outlet = null
            };
        }
    }
    
    /// <summary>
    /// Update an outlet's information
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("updateOutlet")]
    [GraphQLDescription("Update an outlet's information (BrandOwner only)")]
    public async Task<UpdateOutletOutput> UpdateOutlet(
        UpdateOutletInput input,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var outlet = await outletService.UpdateOutletAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                input.Id,
                input.Code,
                input.Name,
                input.Address,
                input.IsActive);
                
            if (outlet == null)
            {
                return new UpdateOutletOutput
                {
                    Success = false,
                    Message = "Outlet not found",
                    Outlet = null
                };
            }
                
            return new UpdateOutletOutput
            {
                Success = true,
                Message = "Outlet updated successfully",
                Outlet = outlet
            };
        }
        catch (Exception ex)
        {
            return new UpdateOutletOutput
            {
                Success = false,
                Message = $"Failed to update outlet: {ex.Message}",
                Outlet = null
            };
        }
    }
    
    /// <summary>
    /// Delete an outlet
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("deleteOutlet")]
    [GraphQLDescription("Delete an outlet (BrandOwner only)")]
    public async Task<DeleteOutletOutput> DeleteOutlet(
        DeleteOutletInput input,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var deleted = await outletService.DeleteOutletAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                input.Id);
                
            if (!deleted)
            {
                return new DeleteOutletOutput
                {
                    Success = false,
                    Message = "Outlet not found or could not be deleted"
                };
            }
                
            return new DeleteOutletOutput
            {
                Success = true,
                Message = "Outlet deleted successfully"
            };
        }
        catch (Exception ex)
        {
            return new DeleteOutletOutput
            {
                Success = false,
                Message = $"Failed to delete outlet: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Grant a user access to an outlet
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("grantUserOutletAccess")]
    [GraphQLDescription("Grant a user access to an outlet (BrandOwner only)")]
    public async Task<GrantOutletAccessOutput> GrantUserOutletAccess(
        GrantOutletAccessInput input,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var granted = await outletService.GrantUserOutletAccessAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                input.UserId,
                input.OutletId);
                
            return new GrantOutletAccessOutput
            {
                Success = granted,
                Message = granted 
                    ? "User granted access to outlet successfully" 
                    : "User already has access to this outlet"
            };
        }
        catch (Exception ex)
        {
            return new GrantOutletAccessOutput
            {
                Success = false,
                Message = $"Failed to grant outlet access: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Revoke a user's access to an outlet
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("revokeUserOutletAccess")]
    [GraphQLDescription("Revoke a user's access to an outlet (BrandOwner only)")]
    public async Task<RevokeOutletAccessOutput> RevokeUserOutletAccess(
        RevokeOutletAccessInput input,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var revoked = await outletService.RevokeUserOutletAccessAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                input.UserId,
                input.OutletId);
                
            return new RevokeOutletAccessOutput
            {
                Success = revoked,
                Message = revoked 
                    ? "User's outlet access revoked successfully" 
                    : "User did not have access to this outlet"
            };
        }
        catch (Exception ex)
        {
            return new RevokeOutletAccessOutput
            {
                Success = false,
                Message = $"Failed to revoke outlet access: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Update a user's outlet access list
    /// </summary>
    [Authorize(Roles = new[] { BrandRoles.BrandOwner })]
    [GraphQLName("updateUserOutlets")]
    [GraphQLDescription("Update a user's complete outlet access list (BrandOwner only)")]
    public async Task<UpdateUserOutletsOutput> UpdateUserOutlets(
        string userId,
        List<string> outletIds,
        [Service] IOutletService outletService,
        [GlobalState] CurrentUser currentUser,
        CancellationToken cancellationToken = default)
    {
        // Require brand context
        if (currentUser?.BrandUser == null || string.IsNullOrEmpty(currentUser.BrandId) || string.IsNullOrEmpty(currentUser.BrandSchema))
        {
            throw new GraphQLException("Brand-context authentication required. Please sign in with brand API credentials.");
        }
        
        try
        {
            var success = await outletService.UpdateUserOutletsAsync(
                currentUser.BrandId,
                currentUser.BrandSchema,
                userId,
                outletIds);
                
            return new UpdateUserOutletsOutput
            {
                Success = success,
                Message = success 
                    ? $"User's outlet access updated to {outletIds.Count} outlets" 
                    : "Failed to update user's outlet access",
                OutletCount = outletIds.Count
            };
        }
        catch (Exception ex)
        {
            return new UpdateUserOutletsOutput
            {
                Success = false,
                Message = $"Failed to update user outlets: {ex.Message}",
                OutletCount = 0
            };
        }
    }
}

#endregion
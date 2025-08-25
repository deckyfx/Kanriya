using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using Kanriya.Server.Data;
using Kanriya.Server.Services.Data;
using Kanriya.Server.Types;
using Kanriya.Server.Types.Inputs;
using Kanriya.Server.Types.Outputs;
using Kanriya.Shared.GraphQL.Types;
using Kanriya.Shared.GraphQL.Payloads;
using Kanriya.Shared.GraphQL.Inputs;

namespace Kanriya.Server.Modules;

/// <summary>
/// User account management module with delete functionality
/// </summary>
[ExtendObjectType("Mutation")]
public class UserAccountModule
{
    /// <summary>
    /// Delete the current user's account and all associated data
    /// </summary>
    [Authorize]
    public async Task<DeleteAccountPayload> DeleteMyAccount(
        DeleteMyAccountInput input,
        [Service] AppDbContext dbContext,
        [Service] IUserService userService,
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser)
    {
        if (currentUser.User == null)
        {
            return new DeleteAccountPayload
            {
                Success = false,
                Message = "User not authenticated"
            };
        }

        // Verify password
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.User.Id);
            
        if (user == null)
        {
            return new DeleteAccountPayload
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
        {
            return new DeleteAccountPayload
            {
                Success = false,
                Message = "Invalid password"
            };
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // Delete all brands owned by the user
            var userBrands = await dbContext.Brands
                .Where(b => b.OwnerId == user.Id)
                .ToListAsync();
                
            foreach (var brand in userBrands)
            {
                await brandService.DeleteBrandAsync(brand.Id);
            }
            
            // Delete user roles
            var userRoles = await dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();
            dbContext.UserRoles.RemoveRange(userRoles);
            
            // Delete the user
            dbContext.Users.Remove(user);
            
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return new DeleteAccountPayload
            {
                Success = true,
                Message = "Account deleted successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            return new DeleteAccountPayload
            {
                Success = false,
                Message = $"Failed to delete account: {ex.Message}"
            };
        }
    }
}
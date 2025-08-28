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
    public async Task<DeleteAccountOutput> DeleteMyAccount(
        DeleteMyAccountInput input,
        [Service] AppDbContext dbContext,
        [Service] IUserService userService,
        [Service] IBrandService brandService,
        [GlobalState] CurrentUser currentUser)
    {
        if (currentUser.User == null)
        {
            return new DeleteAccountOutput
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
            return new DeleteAccountOutput
            {
                Success = false,
                Message = "User not found"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(input.Password, user.PasswordHash))
        {
            return new DeleteAccountOutput
            {
                Success = false,
                Message = "Invalid password"
            };
        }

        using var transaction = await dbContext.Database.BeginTransactionAsync();
        
        try
        {
            // First, get all brands to clean up their PostgreSQL schemas
            var userBrands = await dbContext.Brands
                .Where(b => b.OwnerId == user.Id)
                .ToListAsync();
            
            // Clean up PostgreSQL schemas and users for each brand
            // (The brand records themselves will be deleted by cascade)
            foreach (var brand in userBrands)
            {
                await brandService.CleanupBrandPostgreSQLResourcesAsync(brand);
            }
            
            // Now delete the user (this will cascade delete all brand records)
            dbContext.Users.Remove(user);
            
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            
            return new DeleteAccountOutput
            {
                Success = true,
                Message = "Account deleted successfully"
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            
            return new DeleteAccountOutput
            {
                Success = false,
                Message = $"Failed to delete account: {ex.Message}"
            };
        }
    }
}
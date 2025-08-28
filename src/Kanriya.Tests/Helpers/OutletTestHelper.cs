using System.Text.Json;
using Spectre.Console;

namespace Kanriya.Tests.Helpers;

/// <summary>
/// Helper for outlet-related test operations
/// </summary>
public class OutletTestHelper
{
    public readonly GraphQLClient _client;
    
    public OutletTestHelper(GraphQLClient client)
    {
        _client = client;
    }
    
    /// <summary>
    /// Create a new outlet
    /// </summary>
    public async Task<(bool success, string? outletId, string? message)> CreateOutletAsync(string code, string name, string address)
    {
        var mutation = @"
            mutation CreateOutlet($input: CreateOutletInput!) {
                createOutlet(input: $input) {
                    success
                    message
                    outlet {
                        id
                        code
                        name
                        address
                        isActive
                    }
                }
            }";
            
        var variables = new 
        { 
            input = new 
            { 
                code,
                name,
                address
            } 
        };
        
        var response = await _client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, null, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, null, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, null, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Response data is null");
        }
        
        if (!data.TryGetProperty("createOutlet", out var createOutlet))
        {
            return (false, null, "Invalid response - no createOutlet field");
        }
        
        var success = createOutlet.GetProperty("success").GetBoolean();
        var message = createOutlet.GetProperty("message").GetString();
        
        if (!success)
        {
            return (false, null, message);
        }
        
        var outlet = createOutlet.GetProperty("outlet");
        var outletId = outlet.GetProperty("id").GetString();
        
        LogSuccess($"Created outlet: {name} (Code: {code})");
        return (success, outletId, message);
    }
    
    /// <summary>
    /// List all outlets for the current brand
    /// </summary>
    public async Task<(bool success, int count, List<string>? outletIds, string? message)> ListOutletsAsync()
    {
        var query = @"
            query ListOutlets {
                outlets {
                    id
                    code
                    name
                    address
                    isActive
                }
            }";
            
        var response = await _client.ExecuteAsync(query);
        
        if (response == null)
        {
            return (false, 0, null, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, 0, null, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, 0, null, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, 0, null, "Response data is null");
        }
        
        if (!data.TryGetProperty("outlets", out var outlets))
        {
            return (false, 0, null, "Invalid response - no outlets field");
        }
        
        if (outlets.ValueKind == JsonValueKind.Null)
        {
            return (false, 0, null, "Outlets is null");
        }
        
        var outletList = new List<string>();
        foreach (var outlet in outlets.EnumerateArray())
        {
            outletList.Add(outlet.GetProperty("id").GetString()!);
        }
        
        LogSuccess($"Retrieved {outletList.Count} outlets");
        return (true, outletList.Count, outletList, "Success");
    }
    
    /// <summary>
    /// Update an outlet
    /// </summary>
    public async Task<(bool success, string? message)> UpdateOutletAsync(string id, string? name = null, string? address = null, bool? isActive = null)
    {
        var mutation = @"
            mutation UpdateOutlet($input: UpdateOutletInput!) {
                updateOutlet(input: $input) {
                    success
                    message
                    outlet {
                        id
                        code
                        name
                        address
                        isActive
                    }
                }
            }";
            
        // Build input object with ID and optional fields
        object input;
        if (name != null && address != null && isActive.HasValue)
        {
            input = new { id, name, address, isActive = isActive.Value };
        }
        else if (name != null && address != null)
        {
            input = new { id, name, address };
        }
        else if (name != null && isActive.HasValue)
        {
            input = new { id, name, isActive = isActive.Value };
        }
        else if (address != null && isActive.HasValue)
        {
            input = new { id, address, isActive = isActive.Value };
        }
        else if (name != null)
        {
            input = new { id, name };
        }
        else if (address != null)
        {
            input = new { id, address };
        }
        else if (isActive.HasValue)
        {
            input = new { id, isActive = isActive.Value };
        }
        else
        {
            input = new { id };  // At minimum, we need the ID
        }
        
        // Pass input as single variable
        var variables = new { input };
        
        var response = await _client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }
        
        if (!data.TryGetProperty("updateOutlet", out var updateOutlet))
        {
            return (false, "Invalid response - no updateOutlet field");
        }
        
        var success = updateOutlet.GetProperty("success").GetBoolean();
        var message = updateOutlet.GetProperty("message").GetString();
        
        if (success)
        {
            var outlet = updateOutlet.GetProperty("outlet");
            var outletName = outlet.GetProperty("name").GetString();
            LogSuccess($"Updated outlet: {outletName}");
        }
        else
        {
            LogError($"Failed to update outlet: {message}");
        }
        
        return (success, message);
    }
    
    /// <summary>
    /// Get a single outlet by ID
    /// </summary>
    public async Task<(bool success, string? name, string? message)> GetOutletAsync(string id)
    {
        var query = @"
            query GetOutlet($id: String!) {
                outlet(id: $id) {
                    id
                    code
                    name
                    address
                    isActive
                }
            }";
            
        var variables = new { id };
        
        var response = await _client.ExecuteAsync(query, variables);
        
        if (response == null)
        {
            return (false, null, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, null, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, null, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Response data is null");
        }
        
        if (!data.TryGetProperty("outlet", out var outlet))
        {
            return (false, null, "Invalid response - no outlet field");
        }
        
        if (outlet.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Outlet not found");
        }
        
        var name = outlet.GetProperty("name").GetString();
        LogSuccess($"Retrieved outlet: {name}");
        
        return (true, name, "Success");
    }
    
    /// <summary>
    /// Delete an outlet
    /// </summary>
    public async Task<(bool success, string? message)> DeleteOutletAsync(string id)
    {
        var mutation = @"
            mutation DeleteOutlet($input: DeleteOutletInput!) {
                deleteOutlet(input: $input) {
                    success
                    message
                }
            }";
            
        var variables = new { input = new { id } };
        
        var response = await _client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }
        
        if (!data.TryGetProperty("deleteOutlet", out var deleteOutlet))
        {
            return (false, "Invalid response - no deleteOutlet field");
        }
        
        var success = deleteOutlet.GetProperty("success").GetBoolean();
        var message = deleteOutlet.GetProperty("message").GetString();
        
        if (success)
        {
            LogSuccess("Outlet deleted successfully");
        }
        else
        {
            LogError($"Failed to delete outlet: {message}");
        }
        
        return (success, message);
    }
    
    /// <summary>
    /// List outlets accessible to the current user (BrandOperator)
    /// </summary>
    public async Task<(bool success, int count, List<string>? outletIds, string? message)> ListMyOutletsAsync()
    {
        var query = @"
            query ListMyOutlets {
                myOutlets {
                    id
                    code
                    name
                    address
                    isActive
                }
            }";
            
        var response = await _client.ExecuteAsync(query);
        
        if (response == null)
        {
            return (false, 0, null, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, 0, null, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, 0, null, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, 0, null, "Response data is null");
        }
        
        if (!data.TryGetProperty("myOutlets", out var outlets))
        {
            return (false, 0, null, "Invalid response - no myOutlets field");
        }
        
        if (outlets.ValueKind == JsonValueKind.Null)
        {
            return (false, 0, null, "myOutlets is null");
        }
        
        var outletList = new List<string>();
        foreach (var outlet in outlets.EnumerateArray())
        {
            outletList.Add(outlet.GetProperty("id").GetString()!);
        }
        
        LogSuccess($"Retrieved {outletList.Count} accessible outlets");
        return (true, outletList.Count, outletList, "Success");
    }
    
    /// <summary>
    /// Grant outlet access to a user
    /// </summary>
    public async Task<(bool success, string? message)> GrantOutletAccessAsync(string userId, string outletId)
    {
        var mutation = @"
            mutation GrantOutletAccess($input: GrantOutletAccessInput!) {
                grantOutletAccess(input: $input) {
                    success
                    message
                    userOutlet {
                        userId
                        outletId
                        grantedAt
                    }
                }
            }";
            
        var variables = new 
        { 
            input = new 
            { 
                userId,
                outletId
            } 
        };
        
        var response = await _client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }
        
        if (!data.TryGetProperty("grantOutletAccess", out var grantAccess))
        {
            return (false, "Invalid response - no grantOutletAccess field");
        }
        
        var success = grantAccess.GetProperty("success").GetBoolean();
        var message = grantAccess.GetProperty("message").GetString();
        
        if (success)
        {
            LogSuccess($"Granted outlet access to user {userId}");
        }
        else
        {
            LogError($"Failed to grant outlet access: {message}");
        }
        
        return (success, message);
    }
    
    /// <summary>
    /// Revoke outlet access from a user
    /// </summary>
    public async Task<(bool success, string? message)> RevokeOutletAccessAsync(string userId, string outletId)
    {
        var mutation = @"
            mutation RevokeOutletAccess($input: RevokeOutletAccessInput!) {
                revokeUserOutletAccess(input: $input) {
                    success
                    message
                }
            }";
            
        var variables = new { input = new { userId, outletId } };
        
        var response = await _client.ExecuteAsync(mutation, variables);
        
        if (response == null)
        {
            return (false, "No response from server");
        }
        
        // Check for errors
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, errorMessage);
        }
        
        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, "Invalid response - no data field");
        }
        
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }
        
        if (!data.TryGetProperty("revokeUserOutletAccess", out var revokeAccess))
        {
            return (false, "Invalid response - no revokeUserOutletAccess field");
        }
        
        var success = revokeAccess.GetProperty("success").GetBoolean();
        var message = revokeAccess.GetProperty("message").GetString();
        
        if (success)
        {
            LogSuccess($"Revoked outlet access from user {userId}");
        }
        else
        {
            LogError($"Failed to revoke outlet access: {message}");
        }
        
        return (success, message);
    }
    
    private void LogSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]  ✅ {message}[/]");
    }
    
    private void LogError(string message)
    {
        AnsiConsole.MarkupLine($"[red]  ❌ {message}[/]");
    }
}
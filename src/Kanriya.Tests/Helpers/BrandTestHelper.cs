using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Spectre.Console;

namespace Kanriya.Tests.Helpers;

public class BrandTestHelper
{
    public readonly GraphQLClient _client;

    public BrandTestHelper(GraphQLClient client)
    {
        _client = client;
    }

    public async Task<(bool success, string? brandId, string? apiKey, string? apiPassword, string? message)> CreateBrandAsync(string name)
    {
        var mutation = @"
            mutation CreateBrand($input: CreateBrandInput!) {
                createBrand(input: $input) {
                    success
                    message
                    brand {
                        id
                        name
                    }
                    apiSecret
                    apiPassword
                }
            }";
        var variables = new { input = new { name } };
        var response = await _client.ExecuteAsync(mutation, variables);

        // Handle null response
        if (response == null)
        {
            return (false, null, null, null, "No response from server");
        }

        // Check if response has errors field (authorization errors, etc.)
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            var errorMessage = "Server error";
            if (errors.EnumerateArray().FirstOrDefault().TryGetProperty("message", out var msg))
            {
                errorMessage = msg.GetString() ?? "Server error";
            }
            return (false, null, null, null, errorMessage);
        }

        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, null, null, null, "Invalid response - no data field");
        }

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, null, null, "Response data is null");
        }

        // Get createBrand response
        if (!data.TryGetProperty("createBrand", out var createBrand))
        {
            return (false, null, null, null, "Invalid response - no createBrand field");
        }

        var success = createBrand.GetProperty("success").GetBoolean();
        var message = createBrand.GetProperty("message").GetString();
        
        if (!success)
        {
            return (false, null, null, null, message);
        }

        var brand = createBrand.GetProperty("brand");
        var brandId = brand.GetProperty("id").GetString();
        var apiKey = createBrand.GetProperty("apiSecret").GetString();
        var apiPassword = createBrand.GetProperty("apiPassword").GetString();

        return (success, brandId, apiKey, apiPassword, message);
    }

    public async Task<(bool success, string? message)> DeleteBrandAsync(string brandId)
    {
        var mutation = @"
            mutation DeleteBrand($brandId: String!, $confirmationText: String!) {
                deleteBrand(brandId: $brandId, confirmationText: $confirmationText) {
                    success
                    message
                }
            }";
        var confirmationText = $"DELETE {brandId}";
        var variables = new { brandId, confirmationText };
        var response = await _client.ExecuteAsync(mutation, variables);

        // Handle null response
        if (response == null)
        {
            return (false, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }

        // Get deleteBrand response
        if (!data.TryGetProperty("deleteBrand", out var deleteBrand))
        {
            return (false, "Invalid response - no deleteBrand field");
        }

        var success = deleteBrand.GetProperty("success").GetBoolean();
        var message = deleteBrand.GetProperty("message").GetString();

        return (success, message);
    }

    public async Task<(bool success, string? token, string? message)> SignInBrandAsync(string brandId, string apiKey, string apiPassword)
    {
        var mutation = @"
            mutation SignIn($input: SignInInput!) {
                signIn(input: $input) {
                    success
                    message
                    token
                }
            }";
        // When brandId is provided, email/password are treated as API credentials
        var variables = new { input = new { email = apiKey, password = apiPassword, brandId = brandId } };
        var response = await _client.ExecuteAsync(mutation, variables);

        // Handle null response
        if (response == null)
        {
            return (false, null, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Response data is null");
        }

        // Get signIn response
        if (!data.TryGetProperty("signIn", out var signIn))
        {
            return (false, null, "Invalid response - no signIn field");
        }

        var success = signIn.GetProperty("success").GetBoolean();
        var message = signIn.GetProperty("message").GetString();
        var token = success && signIn.TryGetProperty("token", out var tokenProp) 
            ? tokenProp.GetString() 
            : null;

        // Log JWT token details if successful
        if (success && !string.IsNullOrEmpty(token))
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                
                AnsiConsole.MarkupLine("[blue]JWT Token Details:[/]");
                AnsiConsole.MarkupLine($"[dim]  Bearer Token (first 50 chars): {token?.Substring(0, Math.Min(50, token?.Length ?? 0))}...[/]");
                AnsiConsole.MarkupLine($"[dim]  Token ID: {jsonToken.Id}[/]");
                AnsiConsole.MarkupLine($"[dim]  Issuer: {jsonToken.Issuer}[/]");
                AnsiConsole.MarkupLine($"[dim]  Valid From: {jsonToken.ValidFrom}[/]");
                AnsiConsole.MarkupLine($"[dim]  Valid To: {jsonToken.ValidTo}[/]");
                
                AnsiConsole.MarkupLine("[blue]JWT Claims:[/]");
                foreach (var claim in jsonToken.Claims)
                {
                    AnsiConsole.MarkupLine($"[dim]  {claim.Type}: {claim.Value}[/]");
                }
                
                // Check if this is a brand-context token or principal token
                var hasBrandId = jsonToken.Claims.Any(c => c.Type == "brandId" || c.Type == "brand_id");
                var tokenType = hasBrandId ? "Brand-Context Token" : "Principal Token";
                AnsiConsole.MarkupLine($"[yellow]  Token Type: {tokenType}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to parse JWT token: {ex.Message}[/]");
            }
        }

        return (success, token, message);
    }

    public async Task<(bool success, List<BrandInfoItem>? infoList)> GetBrandInfoAsync()
    {
        var query = @"
            query GetBrandInfo {
                brandInfo {
                    key
                    value
                    createdAt
                    updatedAt
                }
            }";
        var response = await _client.ExecuteAsync(query);

        // Handle null response
        if (response == null)
        {
            return (false, null);
        }

        // Check if response has errors field
        if (response.RootElement.TryGetProperty("errors", out var errors))
        {
            // Brand info requires brand-context authentication
            return (false, null);
        }

        // Check for data field
        if (!response.RootElement.TryGetProperty("data", out var data))
        {
            return (false, null);
        }

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null);
        }

        // Get brandInfo response
        if (!data.TryGetProperty("brandInfo", out var brandInfo))
        {
            return (false, null);
        }

        // Parse the info list
        var infoList = new List<BrandInfoItem>();
        foreach (var item in brandInfo.EnumerateArray())
        {
            var info = new BrandInfoItem
            {
                Key = item.GetProperty("key").GetString() ?? "",
                Value = item.GetProperty("value").GetString() ?? ""
            };
            infoList.Add(info);
        }

        return (true, infoList);
    }

    public async Task<(bool success, string? message)> UpdateBrandInfoAsync(string key, string value)
    {
        var mutation = @"
            mutation UpdateBrandInfo($input: UpdateBrandInfoInput!) {
                updateBrandInfo(input: $input) {
                    success
                    message
                }
            }";
        var variables = new { input = new { key, value } };
        var response = await _client.ExecuteAsync(mutation, variables);

        // Handle null response
        if (response == null)
        {
            return (false, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }

        // Get updateBrandInfo response
        if (!data.TryGetProperty("updateBrandInfo", out var updateBrandInfo))
        {
            return (false, "Invalid response - no updateBrandInfo field");
        }

        var success = updateBrandInfo.GetProperty("success").GetBoolean();
        var message = updateBrandInfo.GetProperty("message").GetString();

        return (success, message);
    }

    public class BrandInfoItem
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public async Task<(bool success, JsonElement? brand, string? message)> GetBrandAsync(string brandId)
    {
        var query = @"
            query GetBrand($brandId: String!) {
                brand(brandId: $brandId) {
                    id
                    name
                    schemaName
                    isActive
                    createdAt
                }
            }";
        var variables = new { brandId };
        var response = await _client.ExecuteAsync(query, variables);

        // Handle null response
        if (response == null)
        {
            return (false, null, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Response data is null");
        }

        // Get brand response
        if (!data.TryGetProperty("brand", out var brand))
        {
            return (false, null, "Invalid response - no brand field");
        }

        return (true, brand, "Brand retrieved successfully");
    }

    public async Task<(bool success, JsonElement? brands, string? message)> GetMyBrandsAsync()
    {
        var query = @"
            query GetMyBrands {
                myBrands {
                    id
                    name
                }
            }";
        var response = await _client.ExecuteAsync(query);

        // Handle null response
        if (response == null)
        {
            return (false, null, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, null, "Response data is null");
        }

        // Get myBrands response
        if (!data.TryGetProperty("myBrands", out var myBrands))
        {
            return (false, null, "Invalid response - no myBrands field");
        }

        return (true, myBrands, null);
    }

    public async Task<(bool success, string? message)> UpdateBrandNameAsync(string brandId, string newName)
    {
        var mutation = @"
            mutation UpdateBrandName($brandId: String!, $newName: String!) {
                updateBrandName(brandId: $brandId, newName: $newName) {
                    success
                    message
                }
            }";
        var variables = new { brandId, newName };
        var response = await _client.ExecuteAsync(mutation, variables);

        // Handle null response
        if (response == null)
        {
            return (false, "No response from server");
        }

        // Check if response has errors field
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

        // Check if data is null
        if (data.ValueKind == JsonValueKind.Null)
        {
            return (false, "Response data is null");
        }

        // Get updateBrandName response
        if (!data.TryGetProperty("updateBrandName", out var updateBrandName))
        {
            return (false, "Invalid response - no updateBrandName field");
        }

        var success = updateBrandName.GetProperty("success").GetBoolean();
        var message = updateBrandName.GetProperty("message").GetString();

        return (success, message);
    }
}
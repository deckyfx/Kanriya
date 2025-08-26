using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Kanriya.Shared.Utils;

/// <summary>
/// Utility class for common string operations
/// </summary>
public static class StringUtils
{
    private const string AlphaNumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    private const string AlphaChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const string NumericChars = "0123456789";
    private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
    
    /// <summary>
    /// Creates a safe string for use as database identifiers (schemas, users, etc.)
    /// Replaces unsafe characters with underscores and converts to lowercase
    /// </summary>
    /// <param name="input">The input string to make safe</param>
    /// <param name="prefix">Optional prefix to add</param>
    /// <param name="toLowerCase">Whether to convert to lowercase (default: true)</param>
    /// <returns>A safe string suitable for database identifiers</returns>
    public static string CreateSafeString(string input, string? prefix = null, bool toLowerCase = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or whitespace", nameof(input));
        
        // Replace hyphens, spaces, and other special characters with underscores
        var safe = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
        
        // Remove consecutive underscores
        safe = Regex.Replace(safe, @"_{2,}", "_");
        
        // Trim underscores from start and end
        safe = safe.Trim('_');
        
        // Apply lowercase if requested
        if (toLowerCase)
            safe = safe.ToLowerInvariant();
        
        // Add prefix if provided
        if (!string.IsNullOrEmpty(prefix))
        {
            safe = $"{prefix}_{safe}";
        }
        
        // Ensure it doesn't start with a number (PostgreSQL requirement)
        if (safe.Length > 0 && char.IsDigit(safe[0]))
        {
            safe = "n_" + safe;
        }
        
        return safe;
    }
    
    /// <summary>
    /// Generates random characters of specified length and character set
    /// </summary>
    /// <param name="length">The length of the string to generate</param>
    /// <param name="includeAlpha">Include alphabetic characters</param>
    /// <param name="includeNumeric">Include numeric characters</param>
    /// <param name="includeSpecial">Include special characters</param>
    /// <param name="includeUppercase">Include uppercase letters (only if includeAlpha is true)</param>
    /// <param name="includeLowercase">Include lowercase letters (only if includeAlpha is true)</param>
    /// <returns>A random string of the specified length</returns>
    public static string GenerateRandomChars(
        int length,
        bool includeAlpha = true,
        bool includeNumeric = true,
        bool includeSpecial = false,
        bool includeUppercase = true,
        bool includeLowercase = true)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0", nameof(length));
        
        var charSetBuilder = new StringBuilder();
        
        if (includeAlpha)
        {
            if (includeUppercase)
                charSetBuilder.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (includeLowercase)
                charSetBuilder.Append("abcdefghijklmnopqrstuvwxyz");
        }
        
        if (includeNumeric)
            charSetBuilder.Append(NumericChars);
        
        if (includeSpecial)
            charSetBuilder.Append(SpecialChars);
        
        var charSet = charSetBuilder.ToString();
        
        if (string.IsNullOrEmpty(charSet))
            throw new ArgumentException("At least one character set must be included");
        
        var result = new char[length];
        var bytes = new byte[length * 4]; // More bytes than needed for better distribution
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
            
            for (int i = 0; i < length; i++)
            {
                var index = BitConverter.ToUInt32(bytes, i * 4) % charSet.Length;
                result[i] = charSet[(int)index];
            }
        }
        
        return new string(result);
    }
    
    /// <summary>
    /// Generates a simple alphanumeric random string (convenience method)
    /// </summary>
    /// <param name="length">The length of the string to generate</param>
    /// <returns>A random alphanumeric string</returns>
    public static string GenerateRandomAlphaNumeric(int length)
    {
        return GenerateRandomChars(length, true, true, false);
    }
    
    /// <summary>
    /// Generates a secure password with mixed case, numbers, and special characters
    /// </summary>
    /// <param name="length">The length of the password (minimum 8)</param>
    /// <returns>A secure random password</returns>
    public static string GenerateSecurePassword(int length = 16)
    {
        if (length < 8)
            throw new ArgumentException("Password length must be at least 8 characters", nameof(length));
        
        // Ensure at least one of each required character type
        var password = new StringBuilder();
        
        // Add one uppercase letter
        password.Append(GenerateRandomChars(1, true, false, false, true, false));
        
        // Add one lowercase letter
        password.Append(GenerateRandomChars(1, true, false, false, false, true));
        
        // Add one number
        password.Append(GenerateRandomChars(1, false, true, false));
        
        // Add one special character
        password.Append(GenerateRandomChars(1, false, false, true));
        
        // Fill the rest with random characters from all sets
        if (length > 4)
        {
            password.Append(GenerateRandomChars(length - 4, true, true, true));
        }
        
        // Shuffle the password to avoid predictable patterns
        return ShuffleString(password.ToString());
    }
    
    /// <summary>
    /// Generates a random API key (16 character alphanumeric)
    /// </summary>
    /// <returns>A 16 character random API key</returns>
    public static string GenerateApiKey()
    {
        return GenerateRandomAlphaNumeric(16);
    }
    
    /// <summary>
    /// Shuffles the characters in a string randomly
    /// </summary>
    private static string ShuffleString(string input)
    {
        var array = input.ToCharArray();
        var bytes = new byte[array.Length * 4];
        
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
            
            for (int i = array.Length - 1; i > 0; i--)
            {
                var j = BitConverter.ToUInt32(bytes, i * 4) % (i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
        
        return new string(array);
    }
}
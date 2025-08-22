using HotChocolate;

namespace GQLServer.Queries;

// ORGANIZING QUERIES - Greeting Related Queries
// ==============================================
// All greeting-related queries in one file for better organization

[ExtendObjectType("Query")]
public class GreetingQueries
{
    // Basic greeting with name
    public string GetGreet(string name)
        => $"Hello, {name}! Welcome to GraphQL!";

    // Greeting with optional title
    public string GetGreetWithTitle(string name, string? title = null)
    {
        if (!string.IsNullOrEmpty(title))
            return $"Hello, {title} {name}!";
        return $"Hello, {name}!";
    }

    // Personalized greeting with multiple parameters
    public string GetPersonalizedGreet(string name, int age, bool isVip)
    {
        var greeting = isVip ? "Greetings" : "Hello";
        return $"{greeting}, {name}! You are {age} years old.";
    }
    
    // Morning greeting
    public string GetMorningGreet(string name)
        => $"Good morning, {name}!";
    
    // Evening greeting
    public string GetEveningGreet(string name)
        => $"Good evening, {name}!";
}
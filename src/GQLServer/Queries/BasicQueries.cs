using HotChocolate;

namespace GQLServer.Queries;

// BASIC QUERIES - Following project structure convention
// =======================================================
// All query declarations must be in the /Queries folder

[ExtendObjectType("Query")]
public class BasicQueries
{
    // Simple hello world query
    public string GetHello() => "world";
    
    // Simple foo bar query
    public string GetFoo() => "bar";
    
    // Query with single argument
    public string GetGreet(string name)
        => $"Hello, {name}! Welcome to GraphQL!";
    
    // Query with optional argument
    public string GetGreetWithTitle(string name, string? title = null)
    {
        if (!string.IsNullOrEmpty(title))
            return $"Hello, {title} {name}!";
        return $"Hello, {name}!";
    }
    
    // Query with multiple arguments
    public string GetPersonalizedGreet(string name, int age, bool isVip)
    {
        var greeting = isVip ? "Greetings" : "Hello";
        return $"{greeting}, {name}! You are {age} years old.";
    }
}
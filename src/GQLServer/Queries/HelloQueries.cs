using HotChocolate;

namespace GQLServer.Queries;

// ORGANIZING QUERIES - PATTERN 1: Separate Files by Feature
// ==========================================================
// Each file contains related queries. This file has all "Hello" related queries.

[ExtendObjectType("Query")]
public class HelloQueries
{
    // Simple hello world query
    public string GetHello() => "world";
    
    // Simple foo bar query  
    public string GetFoo() => "bar";
}
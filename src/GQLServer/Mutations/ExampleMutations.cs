using HotChocolate;
using GQLServer.Types.Inputs;
using GQLServer.Types.Outputs;

namespace GQLServer.Mutations;

// MUTATIONS - For modifying data
// ===============================
// Mutations are GraphQL operations that change data (like POST, PUT, DELETE in REST)

[ExtendObjectType("Mutation")]
public class ExampleMutations
{
    // Simple mutation example - creates a message and returns it
    public string CreateMessage(string content)
    {
        // In a real app, you would save to database here
        return $"Message created: {content}";
    }
    
    // Mutation using input type from Types/Inputs folder
    public SimpleResponse CreateMessageWithInput(CreateMessageInput input)
    {
        // In a real app, you would save to database here
        Console.WriteLine($"Creating message from {input.Author}: {input.Content}");
        
        return new SimpleResponse
        {
            Success = true,
            Message = $"Message from {input.Author} created successfully"
        };
    }
    
    // Mutation with multiple parameters
    public bool UpdateMessage(int id, string newContent)
    {
        // In a real app, you would update the database here
        Console.WriteLine($"Updating message {id} with: {newContent}");
        return true; // Return success status
    }
    
    // Mutation that returns an object type from Types/Outputs folder
    public SimpleResponse DeleteMessage(int id)
    {
        // In a real app, you would delete from database
        return new SimpleResponse
        {
            Success = true,
            Message = $"Message {id} deleted successfully"
        };
    }
}
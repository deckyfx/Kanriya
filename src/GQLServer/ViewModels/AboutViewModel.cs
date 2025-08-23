namespace GQLServer.ViewModels;

public class AboutViewModel
{
    public string Title { get; set; } = "About GQLServer";
    public string Version { get; set; } = "1.0.0";
    public string DotNetVersion { get; set; } = Environment.Version.ToString();
    public string HotChocolateVersion { get; set; } = "15.0.0";
    public DateTime BuildDate { get; set; } = DateTime.UtcNow;
    
    public List<TeamMember> TeamMembers { get; set; } = new()
    {
        new TeamMember { Name = "Lead Developer", Role = "Architecture & Backend", Avatar = "👨‍💻" },
        new TeamMember { Name = "Frontend Dev", Role = "UI/UX & GraphQL Client", Avatar = "👩‍💻" },
        new TeamMember { Name = "DevOps Engineer", Role = "Infrastructure & Deployment", Avatar = "🧑‍💻" }
    };
    
    public List<Technology> Technologies { get; set; } = new()
    {
        new Technology { Name = ".NET 9", Category = "Framework", Description = "Modern cross-platform framework" },
        new Technology { Name = "HotChocolate", Category = "GraphQL", Description = "Enterprise-grade GraphQL server" },
        new Technology { Name = "PostgreSQL", Category = "Database", Description = "Advanced open-source database" },
        new Technology { Name = "Entity Framework Core", Category = "ORM", Description = "Modern object-database mapper" },
        new Technology { Name = "JWT", Category = "Security", Description = "JSON Web Token authentication" },
        new Technology { Name = "Docker", Category = "Container", Description = "Container platform for deployment" }
    };
    
    public List<Milestone> Milestones { get; set; } = new()
    {
        new Milestone { Date = new DateTime(2024, 1, 1), Title = "Project Started", Description = "Initial project setup" },
        new Milestone { Date = new DateTime(2024, 3, 15), Title = "GraphQL Integration", Description = "HotChocolate implementation" },
        new Milestone { Date = new DateTime(2024, 6, 1), Title = "Authentication Added", Description = "JWT auth system" },
        new Milestone { Date = new DateTime(2024, 8, 23), Title = "Current Release", Description = "Production ready" }
    };
}

public class TeamMember
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Avatar { get; set; } = "";
}

public class Technology
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
}

public class Milestone
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
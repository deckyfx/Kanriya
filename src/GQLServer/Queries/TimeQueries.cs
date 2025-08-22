using HotChocolate;

namespace GQLServer.Queries;

// ORGANIZING QUERIES - Time Related Queries
// =========================================================
// All time-related queries in one file

[ExtendObjectType("Query")]
public class TimeQueries
{
    // Get current time
    public string GetCurrentTime()
        => DateTime.Now.ToString("HH:mm:ss");
    
    // Get current date
    public string GetCurrentDate()
        => DateTime.Now.ToString("yyyy-MM-dd");
    
    // Get day of week
    public string GetDayOfWeek()
        => DateTime.Now.DayOfWeek.ToString();
}
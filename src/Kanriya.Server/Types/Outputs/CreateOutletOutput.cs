using Kanriya.Server.Data.BrandSchema;

namespace Kanriya.Server.Types.Outputs;

/// <summary>
/// Response output for outlet creation
/// </summary>
public class CreateOutletOutput
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Message providing details about the operation result
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// The created outlet
    /// </summary>
    public Outlet? Outlet { get; set; }
}
using Microsoft.AspNetCore.Mvc;

namespace Weather.WebUI.Api.Common;

/// <summary>Base for all API controllers: enables MVC API behaviours and JSON-only content.</summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
}

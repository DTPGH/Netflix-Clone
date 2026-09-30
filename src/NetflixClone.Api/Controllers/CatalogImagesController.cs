using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Media;

namespace NetflixClone.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/catalog-images")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class CatalogImagesController(CatalogImageMedia images) : ControllerBase
{
    [HttpGet("{key}")]
    [HttpHead("{key}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public IActionResult Get(string key)
    {
        var image = images.Resolve(key);
        if (image is null) return NotFound();
        Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(image.Value.Path, image.Value.ContentType);
    }
}

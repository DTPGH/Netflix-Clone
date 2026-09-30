using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Application.Admin.Media;
using NetflixClone.Application.Common.Abstractions.Media;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;

namespace NetflixClone.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/media")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminMediaController(IAdminMediaUploadUseCase upload) : ControllerBase
{
    private const long MaximumRequestBytes = AdminMediaUploadUseCase.MaxVideoBytes + 1024 * 1024;

    [HttpPost("images")]
    [RequestSizeLimit(AdminMediaUploadUseCase.MaxImageBytes + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = AdminMediaUploadUseCase.MaxImageBytes + 1024 * 1024)]
    public Task<IActionResult> Image(IFormFile file, CancellationToken cancellationToken)
        => Store(file, AdminMediaKind.Image, cancellationToken);

    [HttpPost("videos")]
    [RequestSizeLimit(MaximumRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaximumRequestBytes)]
    public Task<IActionResult> Video(IFormFile file, CancellationToken cancellationToken)
        => Store(file, AdminMediaKind.Video, cancellationToken);

    private async Task<IActionResult> Store(IFormFile? file, AdminMediaKind kind, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0) return Unauthorized();
        if (file is null) return BadRequest(new { AdminMediaUploadErrors.InvalidFile.Code,
            AdminMediaUploadErrors.InvalidFile.Description });
        await using var content = file.OpenReadStream();
        var result = await upload.ExecuteAsync(new(accountId, kind, file.FileName, file.ContentType,
            file.Length, content), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        return Ok(new { result.Value!.Value, FileName = result.Value.StoredFileName, result.Value.SizeBytes });
    }

    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        ErrorType.Unauthorized => Unauthorized(new { error.Code, error.Description }),
        ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "The media file could not be stored.")
    };
}

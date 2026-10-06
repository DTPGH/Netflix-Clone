using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Profiles;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Profiles;

namespace NetflixClone.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/profiles")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfilesController(IListProfilesUseCase listProfiles, ICreateProfileUseCase createProfile,
    IUpdateProfileUseCase updateProfile, IDeleteProfileUseCase deleteProfile, IProfilePinUseCase pins) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await listProfiles.ExecuteAsync(new(accountId), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) :
            Ok(new ProfilesResponse(result.Value!.Profiles.Select(ProfileResponse.From).ToArray()));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await createProfile.ExecuteAsync(new(accountId, request.Name, request.IsKids, request.AccountPassword), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        // The collection is the available read endpoint; no single-profile GET is introduced.
        return CreatedAtAction(nameof(List), ProfileResponse.From(result.Value!.Profile));
    }

    [HttpPut("{profileId:int:min(1)}")]
    public async Task<IActionResult> Update(int profileId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await updateProfile.ExecuteAsync(new(accountId, profileId, request.Name, request.IsKids, request.AccountPassword), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(ProfileResponse.From(result.Value!.Profile));
    }

    [HttpDelete("{profileId:int:min(1)}")]
    public async Task<IActionResult> Delete(int profileId, [FromBody] ProfilePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await deleteProfile.ExecuteAsync(new(accountId, profileId, request.AccountPassword), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }

    [HttpPut("{profileId:int:min(1)}/pin")]
    public async Task<IActionResult> SetPin(int profileId, SetProfilePinRequest request, CancellationToken ct)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await pins.SetAsync(accountId, profileId, request.AccountPassword, request.Pin, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(ProfileResponse.From(result.Value!));
    }

    [HttpDelete("{profileId:int:min(1)}/pin")]
    public async Task<IActionResult> RemovePin(int profileId, [FromBody] ProfilePasswordRequest request, CancellationToken ct)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await pins.SetAsync(accountId, profileId, request.AccountPassword, null, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(ProfileResponse.From(result.Value!));
    }

    [HttpPost("{profileId:int:min(1)}/unlock")]
    public async Task<IActionResult> Unlock(int profileId, UnlockProfileRequest request, CancellationToken ct)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await pins.UnlockAsync(accountId, profileId, request.Pin, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new UnlockProfileResponse(result.Value!.Token, result.Value.ExpiresAtUtc));
    }

    private bool TryGetAccountId(out int accountId)
        => int.TryParse(User.FindFirst("sub")?.Value, out accountId) && accountId > 0;

    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        ErrorType.Unauthorized => Unauthorized(new { error.Code, error.Description }),
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        ErrorType.Forbidden => StatusCode(403, new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}

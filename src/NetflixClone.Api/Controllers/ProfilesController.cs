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
    IUpdateProfileUseCase updateProfile, IDeleteProfileUseCase deleteProfile) : ControllerBase
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
        var result = await createProfile.ExecuteAsync(new(accountId, request.Name, request.IsKids), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        // The collection is the available read endpoint; no single-profile GET is introduced.
        return CreatedAtAction(nameof(List), ProfileResponse.From(result.Value!.Profile));
    }

    [HttpPut("{profileId:int:min(1)}")]
    public async Task<IActionResult> Update(int profileId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await updateProfile.ExecuteAsync(new(accountId, profileId, request.Name, request.IsKids), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(ProfileResponse.From(result.Value!.Profile));
    }

    [HttpDelete("{profileId:int:min(1)}")]
    public async Task<IActionResult> Delete(int profileId, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await deleteProfile.ExecuteAsync(new(accountId, profileId), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }

    private bool TryGetAccountId(out int accountId)
        => int.TryParse(User.FindFirst("sub")?.Value, out accountId) && accountId > 0;

    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        ErrorType.Unauthorized => Unauthorized(new { error.Code, error.Description }),
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}

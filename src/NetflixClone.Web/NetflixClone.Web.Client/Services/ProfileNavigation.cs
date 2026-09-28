using System.Text.RegularExpressions;
namespace NetflixClone.Web.Client.Services;
public static class ProfileNavigation
{
    public static string Destination(string? returnTo) => returnTo is "/my-list" or "/browse" ||
        Regex.IsMatch(returnTo ?? "", @"\A/movies/[1-9][0-9]*\z") ? returnTo! : "/browse";
    public static string Onboarding(string? returnTo) => "/onboarding?returnTo=" + Uri.EscapeDataString(Destination(returnTo));
}

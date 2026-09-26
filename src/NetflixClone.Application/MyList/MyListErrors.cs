using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.MyList;
public static class MyListErrors
{
    public static readonly Error ProfileNotFound = new("MyList.ProfileNotFound", "The profile was not found.", ErrorType.NotFound);
    public static readonly Error MovieNotFound = new("MyList.MovieNotFound", "The movie was not found.", ErrorType.NotFound);
    public static readonly Error InvalidPage = new("MyList.InvalidPage", "Use page >= 1 and pageSize 1–50.", ErrorType.Validation);
}

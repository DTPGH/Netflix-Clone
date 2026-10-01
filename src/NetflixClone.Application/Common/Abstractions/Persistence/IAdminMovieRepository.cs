using NetflixClone.Application.Admin.Movies;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IAdminMovieRepository
{
    Task<AdminMoviePage> ListAsync(AdminMovieCriteria criteria, CancellationToken cancellationToken = default);
    Task<AdminMovieDetail?> GetAsync(int movieId, CancellationToken cancellationToken = default);
    Task<Movie?> GetForUpdateAsync(int movieId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Genre>> GetGenresAsync(IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken = default);
    Task AddAsync(Movie movie, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminPerson>> SearchPeopleAsync(string search, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Person>> GetPeopleAsync(IReadOnlyCollection<int> personIds, CancellationToken cancellationToken = default);
    void RemoveCredit(MovieCredit credit);
    Task AddPersonAsync(Person person, CancellationToken cancellationToken = default);
}

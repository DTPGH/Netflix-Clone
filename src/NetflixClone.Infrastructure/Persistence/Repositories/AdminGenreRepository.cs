using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Admin.Genres;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class AdminGenreRepository(NetflixCloneDbContext db) : IAdminGenreRepository
{
    public async Task<IReadOnlyList<AdminGenre>> ListAsync(CancellationToken ct) => await db.Genres.AsNoTracking()
        .OrderBy(g => g.Name).ThenBy(g => g.Id).Select(g => new AdminGenre(g.Id, g.Name, g.Movies.Count)).ToListAsync(ct);
    public Task<Genre?> GetAsync(int id, CancellationToken ct) => db.Genres.SingleOrDefaultAsync(g => g.Id == id, ct);
    public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct)
        => db.Genres.AnyAsync(g => g.Name == name && (!exceptId.HasValue || g.Id != exceptId.Value), ct);
    public Task<bool> IsUsedAsync(int id, CancellationToken ct) => db.Genres.Where(g => g.Id == id).AnyAsync(g => g.Movies.Any(), ct);
    public Task<int> MovieCountAsync(int id, CancellationToken ct) => db.Genres.Where(g => g.Id == id).Select(g => g.Movies.Count).SingleAsync(ct);
    public async Task AddAsync(Genre genre, CancellationToken ct) => await db.Genres.AddAsync(genre, ct);
    public void Remove(Genre genre) => db.Genres.Remove(genre);
}

using backend.Domain;
using backend.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Infrastructure.Persistence;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<AppUser?> GetBySpotifyIdAsync(string spotifyId, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.SpotifyId == spotifyId, ct);

    public async Task SaveAsync(AppUser user, CancellationToken ct = default)
    {
        if (user.Id == 0)
            db.Users.Add(user);
        else
            db.Users.Update(user);

        await db.SaveChangesAsync(ct);
    }
}

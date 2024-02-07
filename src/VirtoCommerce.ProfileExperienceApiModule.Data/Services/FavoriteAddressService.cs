using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public interface IFavoriteAddressService
{
    Task AddAddressToFavoritesAsync(string userId, string addressId);
    Task RemoveAddressFromFavoritesAsync(string userId, string addressId);
    Task<IList<string>> GetFavoriteAddressIdsAsync(string userId);
}

public class FavoriteAddressService : IFavoriteAddressService
{
    private readonly Dictionary<string, HashSet<string>> _favoriteAddresses = new();

    public Task AddAddressToFavoritesAsync(string userId, string addressId)
    {
        if (!_favoriteAddresses.TryGetValue(userId, out var ids))
        {
            ids = new HashSet<string>();
            _favoriteAddresses[userId] = ids;
        }

        ids.Add(addressId);

        return Task.CompletedTask;
    }

    public Task RemoveAddressFromFavoritesAsync(string userId, string addressId)
    {
        if (_favoriteAddresses.TryGetValue(userId, out var ids))
        {
            ids.Remove(addressId);
        }

        return Task.CompletedTask;
    }

    public Task<IList<string>> GetFavoriteAddressIdsAsync(string userId)
    {
        IList<string> result = Array.Empty<string>();

        if (_favoriteAddresses.TryGetValue(userId, out var ids))
        {
            result = ids.ToList();
        }

        // TODO: Implement
        return Task.FromResult(result);
    }
}

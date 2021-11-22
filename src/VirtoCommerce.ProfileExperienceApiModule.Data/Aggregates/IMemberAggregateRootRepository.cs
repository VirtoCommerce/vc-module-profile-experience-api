using System.Threading.Tasks;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public interface IMemberAggregateRootRepository
    {
        Task<T> GetMemberAggregateRootByIdAsync<T>(string id) where T : class, IMemberAggregateRoot;
        Task SaveAsync(IMemberAggregateRoot aggregate);
        Task DeleteAsync(string id);
    }
}

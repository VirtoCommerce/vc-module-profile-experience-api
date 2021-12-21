using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public interface IMemberAggregateFactory
    {
        T Create<T>(Member member) where T : class, IMemberAggregateRoot;
    }
}

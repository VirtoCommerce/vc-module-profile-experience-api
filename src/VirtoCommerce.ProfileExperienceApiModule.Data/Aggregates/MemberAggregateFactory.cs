using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public class MemberAggregateFactory : IMemberAggregateFactory
    {
        public virtual T Create<T>(Member member) where T : class, IMemberAggregateRoot
        {
            var result = default(T);

            if (member != null)
            {
                result = member.MemberType switch
                {
                    nameof(Organization) => (T)(object)AbstractTypeFactory<OrganizationAggregate>.TryCreateInstance(),
                    _ => (T)(object)AbstractTypeFactory<ContactAggregate>.TryCreateInstance()
                };

                result.Member = member;
            }

            return result;
        }
    }
}

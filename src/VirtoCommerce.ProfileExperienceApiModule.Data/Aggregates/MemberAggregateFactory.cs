using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public class MemberAggregateFactory : IMemberAggregateFactory
    {
        public virtual T Create<T>(Member member)
            where T : class, IMemberAggregateRoot
        {
            var result = default(T);

            if (member != null)
            {
                result = member.MemberType switch
                {
                    nameof(CustomerModule.Core.Model.Contact) => (T)(object)AbstractTypeFactory<ContactAggregate>.TryCreateInstance(),
                    nameof(CustomerModule.Core.Model.Organization) => (T)(object)AbstractTypeFactory<OrganizationAggregate>.TryCreateInstance(),
                    nameof(CustomerModule.Core.Model.Vendor) => (T)(object)AbstractTypeFactory<VendorAggregate>.TryCreateInstance(),
                };

                result.Member = member;
            }

            return result;
        }
    }
}

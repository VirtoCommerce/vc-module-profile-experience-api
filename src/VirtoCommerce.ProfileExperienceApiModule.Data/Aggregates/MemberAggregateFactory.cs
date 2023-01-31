using System;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

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
                // This is workaround for addresses & dynamic properties commands
                if (typeof(T).Name == nameof(MemberAggregateRootBase))
                {
                    result = member.MemberType switch
                    {
                        nameof(CustomerModule.Core.Model.Organization) => (T)(object)AbstractTypeFactory<OrganizationAggregate>.TryCreateInstance(),
                        nameof(CustomerModule.Core.Model.Contact) => (T)(object)AbstractTypeFactory<ContactAggregate>.TryCreateInstance(),
                        _ => throw new ArgumentOutOfRangeException(nameof(member), "Member type isn't supported")
                    };
                }
                else
                {
                    result = AbstractTypeFactory<T>.TryCreateInstance();
                }

                result.Member = member;
            }

            return result;
        }
    }
}

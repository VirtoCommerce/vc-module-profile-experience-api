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

            if (member != null && IsMemberOfAggregateType<T>(member))
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

        // An aggregate wrapping a member of another type would expose a null Contact / Organization
        private static bool IsMemberOfAggregateType<T>(Member member)
        {
            if (typeof(ContactAggregate).IsAssignableFrom(typeof(T)))
            {
                return member is CustomerModule.Core.Model.Contact;
            }

            if (typeof(OrganizationAggregate).IsAssignableFrom(typeof(T)))
            {
                return member is CustomerModule.Core.Model.Organization;
            }

            return true;
        }
    }
}

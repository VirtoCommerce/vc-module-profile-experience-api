using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

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
                result = AbstractTypeFactory<T>.TryCreateInstance();

                result.Member = member;
            }

            return result;
        }
    }
}

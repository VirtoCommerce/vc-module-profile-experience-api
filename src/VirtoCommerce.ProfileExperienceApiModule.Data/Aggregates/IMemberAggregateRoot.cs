using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public interface IMemberAggregateRoot
    {
        Member Member { get; set; }

        MemberAggregateRootBase UpdateAddresses(IList<Address> addresses);

        MemberAggregateRootBase DeleteAddresses(IList<Address> addresses);
    }
}

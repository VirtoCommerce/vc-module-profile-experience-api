using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateMemberAddressesCommand : ICommand<MemberAggregateRootBase>
    {
        public UpdateMemberAddressesCommand(string memberId, IList<Address> addresses)
        {
            MemberId = memberId;
            Addresses = addresses;
        }

        public string MemberId { get; set; }
        public IList<Address> Addresses { get; set; }
    }
}

using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class DeleteMemberAddressesCommand : MemberCommand, ICommand<MemberAggregateRootBase>
    {
        public DeleteMemberAddressesCommand(string memberId, IList<Address> addresses)
        {
            MemberId = memberId;
            Addresses = addresses;
        }

        public IList<Address> Addresses { get; set; }
    }
}

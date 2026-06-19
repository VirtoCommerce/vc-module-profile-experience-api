using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class LockOrganizationContactCommand : ICommand<ContactAggregate>
    {
        public string MemberId { get; set; }
        public string OrganizationId { get; set; }
    }
}

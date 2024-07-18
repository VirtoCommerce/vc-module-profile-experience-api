using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RemoveMemberFromOrganizationCommand : ICommand<ContactAggregate>
    {
        public string ContactId { get; set; }
        public string OrganizationId { get; set; }
    }
}

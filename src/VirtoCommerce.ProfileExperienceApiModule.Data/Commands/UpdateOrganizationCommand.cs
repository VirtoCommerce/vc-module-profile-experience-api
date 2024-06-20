using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.Exp;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateOrganizationCommand : ExpOrganization, ICommand<OrganizationAggregate>
    {
        public UpdateOrganizationCommand()
        {
            MemberType = new Optional<string>(nameof(Organization));
        }
    }
}

using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class CreateOrganizationCommand : ICommand<OrganizationAggregate>
    {
        public string Name { get; set; }
        public IList<Address> Addresses { get; set; }

        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

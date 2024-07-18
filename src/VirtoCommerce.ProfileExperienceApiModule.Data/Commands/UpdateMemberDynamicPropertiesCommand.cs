using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateMemberDynamicPropertiesCommand : ICommand<IMemberAggregateRoot>
    {
        public string MemberId { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

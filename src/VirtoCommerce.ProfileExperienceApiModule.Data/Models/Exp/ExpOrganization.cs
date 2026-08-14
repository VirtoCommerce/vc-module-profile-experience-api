using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.Exp
{
    public class ExpOrganization
    {
        public string Id { get; set; }

        public Optional<string> MemberType { get; set; }
        public Optional<string> Name { get; set; }

        public IList<Address> Addresses { get; set; }
        public IList<string> Phones { get; set; }
        public IList<string> Emails { get; set; }
        public IList<string> Groups { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class RegisteredOrganization
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string PhoneNumber { get; set; }
        public Address Address { get; set; }
        public IList<Address> Addresses { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

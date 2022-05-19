using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany
{
    public class Company
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Address Address { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

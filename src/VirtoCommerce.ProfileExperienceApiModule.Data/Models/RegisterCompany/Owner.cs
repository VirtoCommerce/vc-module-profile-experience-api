using System;
using System.Collections.Generic;
using VirtoCommerce.ExperienceApiModule.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany
{
    public class Owner
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime Birthdate { get; set; }
        public string PhoneNumber { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
    }
}

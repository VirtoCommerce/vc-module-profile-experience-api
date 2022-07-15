using System;
using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class RegisteredContact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public DateTime Birthdate { get; set; }
        public string PhoneNumber { get; set; }
        public string About { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }
        public Address Address { get; set; }
    }
}

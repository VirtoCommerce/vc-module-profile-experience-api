using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Models;
using VirtoCommerce.Platform.Core.Common;

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

        public Organization MapTo(Organization organization)
        {
            if (organization == null)
            {
                organization = AbstractTypeFactory<Organization>.TryCreateInstance();
            }

            organization.Id = Id;

            if (Name?.IsSpecified == true)
            {
                organization.Name = Name.Value;
            }

            if (MemberType?.IsSpecified == true)
            {
                organization.MemberType = MemberType.Value;
            }

            if (Addresses != null)
            {
                organization.Addresses = Addresses;
            }

            if (Phones != null)
            {
                organization.Phones = Phones;
            }

            if (Emails != null)
            {
                organization.Emails = Emails;
            }

            if (Groups != null)
            {
                organization.Groups = Groups;
            }

            return organization;
        }

    }
}

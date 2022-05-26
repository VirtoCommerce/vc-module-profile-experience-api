using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;
using VirtoCommerce.TaxModule.Core.Model;
using Address = VirtoCommerce.CustomerModule.Core.Model.Address;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Mapping
{
    public class ProfileMappingProfile : AutoMapper.Profile
    {
        public ProfileMappingProfile()
        {
            CreateMap<Contact, Customer>();

            CreateMap<CustomerModule.Core.Model.Address, TaxModule.Core.Model.Address>();

            CreateMap<CreateOrganizationCommand, Organization>()
                .ConvertUsing((command, org, context) =>
                {
                    org = new Organization { Name = command.Name, Addresses = command.Addresses };
                    return org;
                });

            CreateMap<UpdateOrganizationCommand, Organization>()
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<CreateContactCommand, Contact>()
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<UpdateContactCommand, Contact>().ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<Company, Organization>()
                .ConvertUsing((company, organization) =>
                {
                    organization = new Organization()
                    {
                        Name = company.Name,
                        Description = company.Description,
                        Addresses = new List<Address> { company.Address }
                    };

                    return organization;
                });

            CreateMap<Owner, Contact>()
                .ConvertUsing((owner, contact) =>
                {
                    contact = new Contact()
                    {
                        FirstName = owner.FirstName,
                        LastName = owner.LastName,
                        MiddleName = owner.MiddleName,
                        BirthDate = owner.Birthdate,
                        Phones = new List<string> { owner.PhoneNumber },
                    };

                    return contact;
                });
        }
    }
}

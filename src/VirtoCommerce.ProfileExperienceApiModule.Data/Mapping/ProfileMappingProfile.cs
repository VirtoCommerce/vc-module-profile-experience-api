using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
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

            CreateMap<CreateOrganizationCommand, CustomerModule.Core.Model.Organization>()
                .ConvertUsing((command, org, context) =>
                {
                    org = new CustomerModule.Core.Model.Organization { Name = command.Name, Addresses = command.Addresses };
                    return org;
                });

            CreateMap<UpdateOrganizationCommand, CustomerModule.Core.Model.Organization>()
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<CreateContactCommand, Contact>()
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<UpdateContactCommand, Contact>().ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<RegisteredOrganization, Organization>()
                .ConvertUsing((input, result) =>
                {
                    result = new Organization()
                    {
                        Name = input.Name,
                        Description = input.Description,
                        Addresses = input.Address == null ?
                            null :
                            new List<Address> { input.Address }
                    };

                    return result;
                });

            CreateMap<RegisteredContact, Contact>()
                .ConvertUsing((input, result) =>
                {
                    result = new Contact()
                    {
                        FirstName = input.FirstName,
                        LastName = input.LastName,
                        MiddleName = input.MiddleName,
                        BirthDate = input.Birthdate,
                        About = input.About,
                        Phones = input.PhoneNumber == null ?
                            null :
                            new List<string> { input.PhoneNumber },
                        Addresses = input.Address == null ?
                            null :
                            new List<Address> { input.Address }
                    };

                    return result;
                });
        }
    }
}

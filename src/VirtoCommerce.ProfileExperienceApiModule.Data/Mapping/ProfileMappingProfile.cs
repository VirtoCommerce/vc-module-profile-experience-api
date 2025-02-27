using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
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

            CreateMap<Address, TaxModule.Core.Model.Address>();

            CreateMap<CreateOrganizationCommand, Organization>()
                .ConvertUsing((command, organization, context) =>
                {
                    organization = AbstractTypeFactory<Organization>.TryCreateInstance();
                    organization.Name = command.Name;
                    organization.Addresses = command.Addresses;

                    return organization;
                });

            CreateMap<UpdateOrganizationCommand, Organization>()
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<CreateContactCommand, Contact>()
                .ForMember(x => x.Id, opt => opt.Ignore())
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore());

            CreateMap<UpdateContactCommand, Contact>()
                .ForMember(x => x.DynamicProperties, opt => opt.Ignore())
                .ForMember(x => x.Addresses, opt => opt.Condition(x => x.Addresses != null))
                .ForMember(x => x.Emails, opt => opt.Condition(x => x.Emails != null))
                .ForMember(x => x.Groups, opt => opt.Condition(x => x.Groups != null))
                .ForMember(x => x.Phones, opt => opt.Condition(x => x.Phones != null))
                .ForMember(x => x.Organizations, opt => opt.Condition(x => x.Organizations != null));

            CreateMap<RegisteredOrganization, Organization>()
                .ConvertUsing((input, result) =>
                {
                    result = AbstractTypeFactory<Organization>.TryCreateInstance();
                    result.Name = input.Name;
                    result.Description = input.Description;

                    result.Addresses = input.Address == null ?
                            null :
                            new List<Address> { input.Address };

                    return result;
                });

            CreateMap<RegisteredContact, Contact>()
                .ConvertUsing((input, result) =>
                {
                    result = AbstractTypeFactory<Contact>.TryCreateInstance();
                    result.FirstName = input.FirstName;
                    result.LastName = input.LastName;
                    result.MiddleName = input.MiddleName;
                    result.BirthDate = input.Birthdate;
                    result.About = input.About;

                    result.Phones = input.PhoneNumber == null ?
                            null :
                            new List<string> { input.PhoneNumber };

                    result.Addresses = input.Address == null ?
                            null :
                            new List<Address> { input.Address };

                    return result;
                });
        }
    }
}

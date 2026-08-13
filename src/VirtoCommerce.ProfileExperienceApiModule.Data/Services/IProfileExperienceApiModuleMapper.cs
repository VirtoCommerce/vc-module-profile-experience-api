using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public interface IProfileExperienceApiModuleMapper
{
    TaxModule.Core.Model.Customer ToCustomer(Contact source);

    TaxModule.Core.Model.Address ToTaxAddress(Address source);

    Organization ToOrganization(CreateOrganizationCommand source);

    Organization ToOrganization(RegisteredOrganization source);

    Contact ToContact(CreateContactCommand source);

    Contact ToContact(RegisteredContact source);

    void MapTo(UpdateContactCommand source, Contact target);

    void MapTo(UpdateOrganizationCommand source, Organization target);
}

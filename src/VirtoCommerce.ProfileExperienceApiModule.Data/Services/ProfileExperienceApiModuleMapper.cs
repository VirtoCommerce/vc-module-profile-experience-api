using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public class ProfileExperienceApiModuleMapper : IProfileExperienceApiModuleMapper
{
    public virtual TaxModule.Core.Model.Customer ToCustomer(Contact source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<TaxModule.Core.Model.Customer>.TryCreateInstance();

        result.Id = source.Id;
        result.Name = source.Name;
        result.FirstName = source.FirstName;
        result.MiddleName = source.MiddleName;
        result.LastName = source.LastName;
        result.OuterId = source.OuterId;
        result.Addresses = source.Addresses?.Select(ToTaxAddress).ToList();
        result.Phones = source.Phones;
        result.Emails = source.Emails;
        result.Groups = source.Groups;
        result.BirthDate = source.BirthDate;
        result.DefaultLanguage = source.DefaultLanguage;
        result.TimeZone = source.TimeZone;
        result.Organizations = source.Organizations;
        result.TaxPayerId = source.TaxPayerId;

        return result;
    }

    public virtual TaxModule.Core.Model.Address ToTaxAddress(Address source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<TaxModule.Core.Model.Address>.TryCreateInstance();

        result.AddressType = source.AddressType;
        result.Key = source.Key;
        result.Name = source.Name;
        result.Organization = source.Organization;
        result.CountryCode = source.CountryCode;
        result.CountryName = source.CountryName;
        result.City = source.City;
        result.PostalCode = source.PostalCode;
        result.Zip = source.Zip;
        result.Line1 = source.Line1;
        result.Line2 = source.Line2;
        result.RegionId = source.RegionId;
        result.RegionName = source.RegionName;
        result.FirstName = source.FirstName;
        result.MiddleName = source.MiddleName;
        result.LastName = source.LastName;
        result.Phone = source.Phone;
        result.Email = source.Email;
        result.OuterId = source.OuterId;
        result.IsDefault = source.IsDefault;
        result.Description = source.Description;

        return result;
    }

    public virtual Organization ToOrganization(CreateOrganizationCommand source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<Organization>.TryCreateInstance();

        result.Name = source.Name;
        result.Addresses = source.Addresses;

        return result;
    }

    public virtual Organization ToOrganization(RegisteredOrganization source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<Organization>.TryCreateInstance();

        result.Name = source.Name;
        result.Description = source.Description;
        result.Phones = source.PhoneNumber == null ? null : new List<string> { source.PhoneNumber };
        result.Addresses = source.Addresses?.ToList() ?? [];

        if (source.Address != null)
        {
            result.Addresses.Add(source.Address);
        }

        return result;
    }

    public virtual Contact ToContact(CreateContactCommand source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<Contact>.TryCreateInstance();

        result.Name = source.Name;
        result.MemberType = source.MemberType;
        result.PhotoUrl = source.PhotoUrl;
        result.TimeZone = source.TimeZone;
        result.DefaultLanguage = source.DefaultLanguage;
        result.CurrencyCode = source.CurrencyCode;
        result.LastName = source.LastName;
        result.MiddleName = source.MiddleName;
        result.FirstName = source.FirstName;
        result.FullName = source.FullName;
        result.Salutation = source.Salutation;
        result.About = source.About;
#pragma warning disable VC0011 // Contact.SelectedAddressId is obsolete but AutoMapper's convention map still copied it; preserved for parity.
        result.SelectedAddressId = source.SelectedAddressId;
#pragma warning restore VC0011
        result.Addresses = source.Addresses;
        result.Phones = source.Phones;
        result.Emails = source.Emails;
        result.Groups = source.Groups;
        result.Organizations = source.Organizations;

        return result;
    }

    public virtual Contact ToContact(RegisteredContact source)
    {
        if (source == null)
        {
            return null;
        }

        var result = AbstractTypeFactory<Contact>.TryCreateInstance();

        result.FirstName = source.FirstName;
        result.LastName = source.LastName;
        result.MiddleName = source.MiddleName;
        result.BirthDate = source.Birthdate;
        result.About = source.About;
        result.Phones = source.PhoneNumber == null ? null : new List<string> { source.PhoneNumber };
        result.Addresses = source.Address == null ? null : new List<Address> { source.Address };

        return result;
    }

    public virtual void MapTo(UpdateContactCommand source, Contact target)
    {
        if (source == null)
        {
            return;
        }

        target.Id = source.Id;
        target.Name = source.Name;
        target.MemberType = source.MemberType;
        target.PhotoUrl = source.PhotoUrl;
        target.TimeZone = source.TimeZone;
        target.DefaultLanguage = source.DefaultLanguage;
        target.CurrencyCode = source.CurrencyCode;
        target.LastName = source.LastName;
        target.MiddleName = source.MiddleName;
        target.FirstName = source.FirstName;
        target.FullName = source.FullName;
        target.Salutation = source.Salutation;
        target.About = source.About;
#pragma warning disable VC0011 // Contact.SelectedAddressId is obsolete but AutoMapper's convention map still copied it; preserved for parity.
        target.SelectedAddressId = source.SelectedAddressId;
#pragma warning restore VC0011

        if (source.Addresses != null)
        {
            target.Addresses = source.Addresses;
        }

        if (source.Phones != null)
        {
            target.Phones = source.Phones;
        }

        if (source.Emails != null)
        {
            target.Emails = source.Emails;
        }

        if (source.Groups != null)
        {
            target.Groups = source.Groups;
        }

        if (source.Organizations != null)
        {
            target.Organizations = source.Organizations;
        }
    }

    public virtual void MapTo(UpdateOrganizationCommand source, Organization target)
    {
        if (source == null)
        {
            return;
        }

        target.Id = source.Id;

        if (source.Name?.IsSpecified == true)
        {
            target.Name = source.Name.Value;
        }

        if (source.MemberType?.IsSpecified == true)
        {
            target.MemberType = source.MemberType.Value;
        }

        if (source.Addresses != null)
        {
            target.Addresses = source.Addresses;
        }

        if (source.Phones != null)
        {
            target.Phones = source.Phones;
        }

        if (source.Emails != null)
        {
            target.Emails = source.Emails;
        }

        if (source.Groups != null)
        {
            target.Groups = source.Groups;
        }
    }
}

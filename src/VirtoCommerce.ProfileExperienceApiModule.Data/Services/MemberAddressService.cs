using System;
using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public class MemberAddressService : IMemberAddressService
{
    public virtual MemberAddress ToMemberAddress(Address address, IList<string> favoriteAddressIds)
    {
        ArgumentNullException.ThrowIfNull(address);

        var result = AbstractTypeFactory<MemberAddress>.TryCreateInstance();

        result.Key = address.Key;
        result.IsDefault = address.IsDefault;
        result.City = address.City;
        result.CountryCode = address.CountryCode;
        result.CountryName = address.CountryName;
        result.Email = address.Email;
        result.FirstName = address.FirstName;
        result.MiddleName = address.MiddleName;
        result.LastName = address.LastName;
        result.Line1 = address.Line1;
        result.Line2 = address.Line2;
        result.Name = address.Name;
        result.Organization = address.Organization;
        result.Phone = address.Phone;
        result.PostalCode = address.PostalCode;
        result.RegionId = address.RegionId;
        result.RegionName = address.RegionName;
        result.Zip = address.Zip;
        result.OuterId = address.OuterId;
        result.Description = address.Description;
        result.AddressType = address.AddressType;

        if (favoriteAddressIds != null)
        {
            result.IsFavorite = favoriteAddressIds.Contains(address.Key);
        }

        return result;
    }
}

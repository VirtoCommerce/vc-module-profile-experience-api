using System.Collections.Generic;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

public class VendorAggregate: MemberAggregateRootBase
{
    public CustomerModule.Core.Model.Contact Contact => Member as CustomerModule.Core.Model.Contact;

    public CustomerModule.Core.Model.Organization Organization => Member as CustomerModule.Core.Model.Organization;

    public CustomerModule.Core.Model.Vendor Vendor => Member as CustomerModule.Core.Model.Vendor;

    public IEnumerable<ExpVendorRating> Ratings { get; set; }
}

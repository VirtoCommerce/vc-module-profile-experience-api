using GraphQL;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentOrganizationAddressesQuery : BaseAddressesQuery
{
    public string OrganizationId { get; set; }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        OrganizationId = context.GetCurrentOrganizationId();
    }
}

using System.Linq;
using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class VendorType : MemberBaseType<VendorAggregate>
{
    public VendorType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        Name = "Vendor";
        Description = "Vendor Info";

        Field<StringGraphType>("about").Description("About vendor")
            .Resolve(context =>
                context.Source.Contact?.About ??
                context.Source.Organization?.Description ??
                context.Source.Vendor?.Description);
        Field<StringGraphType>("iconUrl").Description("Icon URL")
            .Resolve(context =>
                context.Source.Member.IconUrl ??
                context.Source.Contact?.PhotoUrl ??
                context.Source.Vendor?.LogoUrl);
        Field<StringGraphType>("siteUrl").Description("Site URL").Resolve(context => context.Source.Vendor?.SiteUrl);

        ExtendableField<RatingType>(
            "rating",
            "Vendor rating",
            arguments: new QueryArguments(new QueryArgument<StringGraphType>
            {
                Name = "storeId",
                Description = "Filter vendor ratings to return only values for specified store"
            }),
            resolve: context =>
            {
                var storeId = context.GetArgumentOrValue<string>("storeId");
                var result = context.Source.Ratings?.FirstOrDefault(rating => rating.StoreId == storeId);
                return result;
            });
    }
}

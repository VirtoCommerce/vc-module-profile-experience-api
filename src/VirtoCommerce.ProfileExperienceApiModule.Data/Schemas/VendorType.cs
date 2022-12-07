using System.Linq;
using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class VendorType: MemberBaseType<VendorAggregate>
{
    public VendorType(IDynamicPropertyResolverService dynamicPropertyResolverService) : base(dynamicPropertyResolverService)
    {
        Name = "Vendor";
        Description = "Vendor Info";
        
        Field<StringGraphType>("about", description: "About vendor",
            resolve: context =>
                context.Source.Contact.About ?? 
                context.Source.Organization.Description ??
                context.Source.Vendor.Description);
        Field<StringGraphType>("iconUrl", description: "Icon URL",
            resolve: context =>
                context.Source.Member.IconUrl ??
                context.Source.Contact?.PhotoUrl ??
                context.Source.Employee?.PhotoUrl ??
                context.Source.Vendor?.LogoUrl);
        Field<StringGraphType>("siteUrl", description: "Site URL", resolve: context => context.Source.Vendor?.SiteUrl);

        Field(
            GraphTypeExtenstionHelper.GetActualType<RatingType>(),
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

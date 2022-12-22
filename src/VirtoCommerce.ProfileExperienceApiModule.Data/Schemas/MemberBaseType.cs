using GraphQL.Builders;
using GraphQL.Types;
using System;
using System.Linq;
using GraphQL;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.CoreModule.Core.Seo;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public abstract class MemberBaseType<TAggregate> : ExtendableGraphType<TAggregate>
    where TAggregate: MemberAggregateRootBase
{
    protected MemberBaseType(IDynamicPropertyResolverService dynamicPropertyResolverService)
    {
        Field(x => x.Member.Id);
        Field(x => x.Member.OuterId, true).Description("Outer ID");
        Field(x => x.Member.MemberType).Description("Member type");
        Field(x => x.Member.Name, true).Description("Name");
        Field(x => x.Member.Status, true).Description("Status");
        Field(x => x.Member.Phones).Description("Phones");
        Field(x => x.Member.Emails).Description("Emails");
        Field(x => x.Member.Groups);

        #region SEO

        Field(x => x.Member.SeoObjectType).Description("SEO object type");
        Field<SeoInfoType>("seoInfo",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "cultureName" }),
            resolve: context =>
            {
                var source = context.Source;
                var storeId = context.GetArgumentOrValue<string>("storeId");
                var cultureName = context.GetArgumentOrValue<string>("cultureName");

                SeoInfo seoInfo = null;

                if (!source.Member.SeoInfos.IsNullOrEmpty())
                {
                    seoInfo = source.Member.SeoInfos.GetBestMatchingSeoInfo(storeId, cultureName);
                }

                return seoInfo ??
                       SeoInfosExtensions.GetFallbackSeoInfo(source.Member.Id, source.Member.Name, cultureName);
            }, description: "Request related SEO info");

        #endregion

        #region Default addresses

        Field<MemberAddressType>("defaultAddress", description: "Default address",
            resolve: context => context.Source.Member.Addresses.SingleOrDefault(address => address.IsDefault));

        Field<MemberAddressType>("defaultBillingAddress", description: "Default billing address",
            resolve: context => context.Source.Member.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Billing && address.IsDefault));

        Field<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
            resolve: context => context.Source.Member.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Shipping && address.IsDefault));

        #endregion

        #region Addresses

        var addressesConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<MemberAddressType, MemberAggregateRootBase>()
            .Name("addresses")
            .Argument<StringGraphType>("sort", "Sort expression")
            .PageSize(20);

        addressesConnectionBuilder.Resolve(ResolveAddressesConnection);
        AddField(addressesConnectionBuilder.FieldType);

        #endregion

        ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
            "dynamicProperties",
            "Dynamic property values",
            QueryArgumentPresets.GetArgumentForDynamicProperties(),
            context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Member, context.GetArgumentOrValue<string>("cultureName")));
    }

    protected virtual object ResolveAddressesConnection(IResolveConnectionContext<MemberAggregateRootBase> context)
    {
        var take = context.First ?? 20;
        var skip = Convert.ToInt32(context.After ?? 0.ToString());
        var sort = context.GetArgument<string>("sort");
        var addresses = context.Source.Member.Addresses.AsEnumerable();

        if (!string.IsNullOrEmpty(sort))
        {
            var sortInfos = SortInfo.Parse(sort);
            addresses = addresses
                .AsQueryable()
                .OrderBySortInfos(sortInfos);
        }

        return new PagedConnection<Address>(addresses.Skip(skip).Take(take), skip, take, addresses.Count());
    }
}

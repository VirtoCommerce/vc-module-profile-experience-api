using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Seo.Core.Extensions;
using VirtoCommerce.Seo.Core.Models;
using VirtoCommerce.StoreModule.Core.Extensions;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;
using SeoExtensions = VirtoCommerce.Seo.Core.Extensions.SeoExtensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public abstract class MemberBaseType<TAggregate> : ExtendableGraphType<TAggregate>
    where TAggregate : MemberAggregateRootBase
{
    private readonly IMemberAddressService _memberAddressService;

    protected MemberBaseType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService)
    {
        _memberAddressService = memberAddressService;

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

        ExtendableFieldAsync<SeoInfoType>(
            "seoInfo",
            "Request related SEO info",
            new QueryArguments(
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "storeId" },
                new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "cultureName" }),
            resolve: async context =>
            {
                var member = context.Source.Member;
                var storeId = context.GetArgumentOrValue<string>("storeId");
                var cultureName = context.GetArgumentOrValue<string>("cultureName");

                SeoInfo seoInfo = null;

                if (!string.IsNullOrEmpty(storeId) && !member.SeoInfos.IsNullOrEmpty())
                {
                    var store = await storeService.GetByIdAsync(storeId);
                    if (store != null)
                    {
                        seoInfo = member.SeoInfos.GetBestMatchingSeoInfo(store, cultureName);
                    }
                }

                return seoInfo ?? SeoExtensions.GetFallbackSeoInfo(member.Id, member.Name, cultureName);
            });

        #endregion

        #region Default addresses

        ExtendableFieldAsync<MemberAddressType>("defaultBillingAddress", description: "Default billing address",
            resolve: context => ResolveDefaultAddressAsync(context, AddressType.Billing));

        ExtendableFieldAsync<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
            resolve: context => ResolveDefaultAddressAsync(context, AddressType.Shipping));

        #endregion

        #region Addresses

        var addressesConnectionBuilder = GraphTypeExtensionHelper.CreateConnection<MemberAddressType, TAggregate>("addresses")
            .Argument<StringGraphType>("sort", "Sort expression")
            .PageSize(20);

        addressesConnectionBuilder.ResolveAsync(ResolveAddressesConnectionAsync);
        AddField(addressesConnectionBuilder.FieldType);

        #endregion

        ExtendableFieldAsync<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
            "dynamicProperties",
            "Dynamic property values",
            null,
            async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Member, context.GetArgumentOrValue<string>("cultureName")));
    }


    protected virtual async Task<object> ResolveDefaultAddressAsync(IResolveFieldContext<TAggregate> context, AddressType addressType)
    {
        var address = context.Source.Member.Addresses.FirstOrDefault(x => x.IsDefault && x.AddressType == addressType);
        return address is null ? null : await _memberAddressService.ToMemberAddressAsync(address, context.GetCurrentUserId());
    }

    protected virtual async Task<object> ResolveAddressesConnectionAsync(IResolveConnectionContext<TAggregate> context)
    {
        var take = context.First ?? 20;
        var skip = Convert.ToInt32(context.After ?? 0.ToString());
        var sort = context.GetArgument<string>("sort");
        var addresses = context.Source.Member.Addresses;

        var page = (await _memberAddressService.ToMemberAddressesAsync(addresses, context.GetCurrentUserId()))
            .AsQueryable()
            .OrderBySortInfos(BuildAddressSortExpression(sort))
            .Skip(skip)
            .Take(take);

        return new PagedConnection<MemberAddress>(page, skip, take, addresses.Count);
    }

    protected static IEnumerable<SortInfo> BuildAddressSortExpression(string sort)
    {
        const string isFavorite = nameof(MemberAddress.IsFavorite);
        const string name = nameof(MemberAddress.Name);
        const string defaultSorting = $"{isFavorite}:desc;{name}";

        return SortInfo.Parse(sort.EmptyToNull() ?? defaultSorting);
    }
}

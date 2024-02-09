using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public abstract class MemberBaseType<TAggregate> : ExtendableGraphType<TAggregate>
    where TAggregate : MemberAggregateRootBase
{
    private readonly IFavoriteAddressService _favoriteAddressService;

    protected MemberBaseType(
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IFavoriteAddressService favoriteAddressService)
    {
        _favoriteAddressService = favoriteAddressService;

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

        Field<MemberAddressType>("defaultBillingAddress", description: "Default billing address",
            resolve: context => ResolveDefaultAddress(context, AddressType.Billing));

        Field<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
            resolve: context => ResolveDefaultAddress(context, AddressType.Shipping));

        #endregion

        #region Addresses

        var addressesConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<MemberAddressType, TAggregate>()
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


    protected virtual object ResolveDefaultAddress(IResolveFieldContext<TAggregate> context, AddressType addressType)
    {
        var address = context.Source.Member.Addresses.FirstOrDefault(x => x.IsDefault && x.AddressType == addressType);
        return address?.ToMemberAddress(GetFavoriteAddressIds(context));
    }

    protected virtual object ResolveAddressesConnection(IResolveConnectionContext<TAggregate> context)
    {
        var take = context.First ?? 20;
        var skip = Convert.ToInt32(context.After ?? 0.ToString());
        var sort = context.GetArgument<string>("sort");
        var addresses = context.Source.Member.Addresses;

        var favoriteAddressIds = GetFavoriteAddressIds(context);

        var page = addresses
            .Select(x => x.ToMemberAddress(favoriteAddressIds))
            .AsQueryable()
            .OrderBySortInfos(BuildSortExpression(sort))
            .Skip(skip)
            .Take(take);

        return new PagedConnection<MemberAddress>(page, skip, take, addresses.Count);
    }

    protected IList<string> GetFavoriteAddressIds(IResolveFieldContext<TAggregate> context)
    {
        var userId = context.GetCurrentUserId();
        return _favoriteAddressService.GetFavoriteAddressIdsAsync(userId).GetAwaiter().GetResult();
    }

    protected static IEnumerable<SortInfo> BuildSortExpression(string sort)
    {
        const string isFavorite = nameof(MemberAddress.IsFavorite);
        const string name = nameof(MemberAddress.Name);
        const string defaultSorting = $"{isFavorite}:desc;{name}";

        return SortInfo.Parse(sort.EmptyToNull() ?? defaultSorting);
    }
}

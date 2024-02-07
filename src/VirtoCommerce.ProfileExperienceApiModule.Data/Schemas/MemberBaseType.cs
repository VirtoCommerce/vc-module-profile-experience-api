using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using VirtoCommerce.CoreModule.Core.Seo;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
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
            resolve: context => context.Source.Member.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Billing && address.IsDefault));

        Field<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
            resolve: context => context.Source.Member.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Shipping && address.IsDefault));

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

    protected virtual object ResolveAddressesConnection(IResolveConnectionContext<TAggregate> context)
    {
        var take = context.First ?? 20;
        var skip = Convert.ToInt32(context.After ?? 0.ToString());
        var sort = context.GetArgument<string>("sort");
        var addresses = context.Source.Member.Addresses;

        var userId = context.GetCurrentUserId();
        var favoriteAddressIds = _favoriteAddressService.GetFavoriteAddressIdsAsync(userId).GetAwaiter().GetResult();

        var page = addresses
            .Select(x => ToMemberAddress(x, favoriteAddressIds))
            .AsQueryable()
            .OrderBySortInfos(BuildSortExpression(sort))
            .Skip(skip)
            .Take(take);

        return new PagedConnection<MemberAddress>(page, skip, take, addresses.Count);
    }

    private static MemberAddress ToMemberAddress(Address address, IList<string> favoriteAddressIds)
    {
        var result = AbstractTypeFactory<MemberAddress>.TryCreateInstance();

        result.Key = address.Key;
        result.IsDefault = address.IsDefault;
        result.IsFavorite = favoriteAddressIds.Contains(address.Key);
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

        return result;
    }

    private static IEnumerable<SortInfo> BuildSortExpression(string sort)
    {
        const string isFavorite = nameof(MemberAddress.IsFavorite);
        const string sortByFavorite = $"{isFavorite}:desc";

        sort = string.IsNullOrEmpty(sort)
            ? sortByFavorite
            : $"{sortByFavorite};{sort}";

        var sortInfos = SortInfo.Parse(sort);

        return sortInfos;
    }
}

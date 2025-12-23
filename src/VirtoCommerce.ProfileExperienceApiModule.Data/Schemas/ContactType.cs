using System;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class ContactType : MemberBaseType<ContactAggregate>
{
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
    private readonly ICustomerPreferenceService _customerPreferenceService;

    public ContactType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        ICustomerPreferenceService customerPreferenceService,
        IMediator mediator,
        IMemberAggregateFactory memberAggregateFactory)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        _userManagerFactory = userManagerFactory;
        _customerPreferenceService = customerPreferenceService;

        Field(x => x.Contact.FirstName);
        Field(x => x.Contact.LastName);
        Field(x => x.Contact.MiddleName, true);
        Field(x => x.Contact.FullName);

        Field(x => x.Contact.About);

        Field(x => x.Contact.DefaultLanguage, nullable: true)
            .ResolveAsync(async context =>
                context.Source.Contact.DefaultLanguage ?? (await GetStore(context, storeService))?.DefaultLanguage);
        Field(x => x.Contact.CurrencyCode, nullable: true)
            .ResolveAsync(async context =>
                context.Source.Contact.CurrencyCode ?? (await GetStore(context, storeService))?.DefaultCurrency);

        Field<DateGraphType>("birthDate")
            .Resolve(context => context.Source.Contact.BirthDate?.Date);

        Field<ListGraphType<UserType>>("securityAccounts").Resolve(context => context.Source.Contact.SecurityAccounts);

        Field<StringGraphType>("organizationId")
            .ResolveAsync(async context => await GetCurrentOrganizationId(context));

        Field<OrganizationType>("organization")
            .ResolveAsync(async context =>
            {
                var organizationId = await GetCurrentOrganizationId(context);
                if (organizationId.IsNullOrEmpty())
                {
                    return null;
                }

                var query = new GetOrganizationByIdQuery()
                {
                    Id = organizationId,
                };
                return await mediator.Send(query);
            });

        Field<StringGraphType>("selectedAddressId")
            .Description("Selected shipping address id.")
            .ResolveAsync(async context => await GetSelectedAddressId(context));

        #region Organizations

        Field("organizationsIds", x => x.Contact.Organizations);

        var organizationsConnectionBuilder = GraphTypeExtensionHelper
            .CreateConnection<OrganizationType, ContactAggregate>("organizations")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .PageSize(20);

        organizationsConnectionBuilder.ResolveAsync(async context =>
        {
            var response = AbstractTypeFactory<MemberSearchResult>.TryCreateInstance();
            var query = context.GetSearchMembersQuery<SearchOrganizationsQuery>();

            // If user have no organizations, member search service would return all organizations
            // it means we don't need the search request when user's organization list is empty
            if (!context.Source.Contact.Organizations.IsNullOrEmpty())
            {
                query.DeepSearch = true;
                query.ObjectIds = context.Source.Contact.Organizations;
                response = await mediator.Send(query);
            }

            return new PagedConnection<OrganizationAggregate>(
                response.Results.Select(memberAggregateFactory.Create<OrganizationAggregate>), query.Skip,
                query.Take, response.TotalCount);
        });
        AddField(organizationsConnectionBuilder.FieldType);

        #endregion
    }

    private static async Task<Store> GetStore(IResolveFieldContext<ContactAggregate> context, IStoreService storeService)
    {
        return await storeService.GetByIdAsync(context.Source.StoreId);
    }

    private async Task<string> GetCurrentOrganizationId(IResolveFieldContext<ContactAggregate> context)
    {
        if (!await IsCurrentUser(context))
        {
            return null;
        }

        return context.GetCurrentOrganizationId();
    }

    private async Task<string> GetSelectedAddressId(IResolveFieldContext<ContactAggregate> context)
    {
        if (!await IsCurrentUser(context))
        {
            return null;
        }

        return await _customerPreferenceService.GetSelectedAddressId(context.GetCurrentUserId(), context.GetCurrentOrganizationId());
    }

    private async Task<bool> IsCurrentUser(IResolveFieldContext<ContactAggregate> context)
    {
        return context.Source.Contact.Id.EqualsIgnoreCase(await GetCurrentMemberId(context));
    }

    private async Task<string> GetCurrentMemberId(IResolveFieldContext<ContactAggregate> context)
    {
        const string contextKey = "CurrentUserMemberId";

        if (context.UserContext.TryGetValue(contextKey, out var contextValue))
        {
            return contextValue as string;
        }

        var userId = context.GetCurrentUserId();
        if (userId.IsNullOrEmpty())
        {
            return null;
        }

        using var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(userId);
        var currentMemberId = user?.MemberId;

        context.UserContext[contextKey] = currentMemberId;

        return currentMemberId;
    }
}

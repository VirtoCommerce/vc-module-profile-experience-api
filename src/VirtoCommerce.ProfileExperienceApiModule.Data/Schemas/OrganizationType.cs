using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.DataLoader;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
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
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class OrganizationType : MemberBaseType<OrganizationAggregate>
{
    public OrganizationType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        IMemberAggregateFactory factory,
        IMemberService memberService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        IDataLoaderContextAccessor dataLoader)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        Name = "Organization";
        Description = "Organization info";

        Field(x => x.Organization.Description, true).Description("Description");
        Field(x => x.Organization.BusinessCategory, true).Description("Business category");
        Field(x => x.Organization.OwnerId, true).Description("Owner id");
        Field(x => x.Organization.ParentId, true).Description("Parent id");

        Field<StringGraphType>("myStatusInOrganization")
            .Description("Current user's effective status in this organization: the organization-specific override if set, otherwise the contact's global status.")
            .Resolve(context => ResolveMyStatusInOrganization(context, organizationMembershipService, memberService, userManagerFactory, dataLoader));

        Field<ListGraphType<RoleType>>("contactRoles")
            .Description("Distinct roles currently assigned to at least one member of this organization - " +
                          "membership roles and members' global (account-level) roles alike. Useful for " +
                          "building a members-list role filter that only offers roles with results.")
            .Argument<StringGraphType>("storeId", "Store ID")
            .Argument<StringGraphType>("cultureName", "Culture name for localized responses")
            .ResolveAsync(async context => await context.GetMediator().Send(
                new GetOrganizationContactRolesQuery
                {
                    OrganizationId = context.Source.Organization.Id,
                    StoreId = context.GetArgument<string>("storeId"),
                    CultureName = context.GetArgument<string>("cultureName"),
                }));

        Field<ListGraphType<RoleType>>("assignableRoles")
            .Description("Real platform roles a company member can be assigned in this store - the intersection " +
                          "of the store's role whitelist and the roles that actually exist, unlike contactRoles " +
                          "this includes roles nobody has yet and excludes whitelist entries that don't resolve " +
                          "to a real role.")
            .Argument<StringGraphType>("storeId", "Store ID")
            .Argument<StringGraphType>("cultureName", "Culture name for localized responses")
            .ResolveAsync(async context => await context.GetMediator().Send(
                new GetAssignableCompanyRolesQuery
                {
                    StoreId = context.GetArgument<string>("storeId"),
                    CultureName = context.GetArgument<string>("cultureName"),
                }));

        var connectionBuilder = GraphTypeExtensionHelper.CreateConnection<ContactType, OrganizationAggregate>("contacts")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .Argument<ListGraphType<StringGraphType>>("roleIds", "Filter contacts by role IDs (org-level, membership, or global)")
            .Argument<ListGraphType<StringGraphType>>("statuses", "Filter contacts by effective status/lock state for this organization (e.g. Approved, Invited, Locked)")
            .Argument<StringGraphType>("storeId", "Store ID")
            .Argument<StringGraphType>("cultureName", "Culture name for localized responses")
            .PageSize(20);

        connectionBuilder.ResolveAsync(context => ResolveContactsConnectionAsync(context, factory));
        AddField(connectionBuilder.FieldType);
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public OrganizationType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        IMediator mediator,
        IMemberAggregateFactory factory,
        IMemberService memberService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        IDataLoaderContextAccessor dataLoader)
        : this(storeService, dynamicPropertyResolverService, memberAddressService, factory, memberService, organizationMembershipService, userManagerFactory, dataLoader)
    {
    }

    private static async Task<object> ResolveContactsConnectionAsync(
        IResolveConnectionContext<OrganizationAggregate> context,
        IMemberAggregateFactory factory)
    {
        var query = context.GetSearchMembersQuery<SearchContactsQuery>();
        var orgId = context.Source.Organization.Id;
        query.MemberId = orgId;
        query.DeepSearch = false;

        var mediator = context.GetMediator();
        var storeId = context.GetArgument<string>("storeId");
        var cultureName = context.GetArgument<string>("cultureName");

        var roleIds = context.GetArgument<IList<string>>("roleIds");
        if (roleIds is { Count: > 0 })
        {
            var roleFilter = await mediator.Send(new ResolveOrganizationRoleFilterQuery
            {
                OrganizationId = orgId,
                RoleIds = roleIds,
                StoreId = storeId,
                CultureName = cultureName,
            });

            if (roleFilter.FilterRequired)
            {
                if (roleFilter.Ids.Count == 0)
                {
                    return new PagedConnection<ContactAggregate>([], query.Skip, query.Take, 0);
                }

                query.ObjectIds = IntersectObjectIds(query.ObjectIds, roleFilter.Ids);
            }
        }

        var statuses = context.GetArgument<IList<string>>("statuses");
        if (statuses is { Count: > 0 })
        {
            var statusFilter = await mediator.Send(new ResolveOrganizationStatusFilterQuery
            {
                OrganizationId = orgId,
                Statuses = statuses,
                StoreId = storeId,
                CultureName = cultureName,
            });

            if (statusFilter.FilterRequired)
            {
                if (statusFilter.Ids.Count == 0)
                {
                    return new PagedConnection<ContactAggregate>([], query.Skip, query.Take, 0);
                }

                query.ObjectIds = IntersectObjectIds(query.ObjectIds, statusFilter.Ids);
            }
        }

        var response = await mediator.Send(query);

        return new PagedConnection<ContactAggregate>(
            response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take,
            response.TotalCount);
    }

    private static IDataLoaderResult<string> ResolveMyStatusInOrganization(
        IResolveFieldContext<OrganizationAggregate> context,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IMemberService memberService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        IDataLoaderContextAccessor dataLoader)
    {
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, string>(
            "organization_myStatusInOrg",
            async organizationIds => await ResolveMyStatusesByOrganizationAsync(
                context, organizationMembershipSearchService, memberService, userManagerFactory, organizationIds));

        return loader.LoadAsync(context.Source.Organization.Id);
    }

    private static async Task<IDictionary<string, string>> ResolveMyStatusesByOrganizationAsync(
        IResolveFieldContext<OrganizationAggregate> context,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IMemberService memberService,
        Func<UserManager<ApplicationUser>> userManagerFactory,
        IEnumerable<string> organizationIds)
    {
        var userId = context.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return new Dictionary<string, string>();
        }

        using var userManager = userManagerFactory();
        var user = await userManager.FindByIdAsync(userId);
        if (string.IsNullOrEmpty(user?.MemberId))
        {
            return new Dictionary<string, string>();
        }

        var member = await memberService.GetByIdAsync(user.MemberId);
        var globalStatus = member?.Status;

        var idsList = organizationIds.ToList();
        var memberships = await organizationMembershipSearchService.SearchAllNoCloneAsync(new OrganizationMembershipSearchCriteria
        {
            UserId = userId,
            OrganizationIds = idsList,
        });

        var membershipByOrgId = memberships
            .Where(m => m.OrganizationId != null)
            .GroupBy(m => m.OrganizationId)
            .ToDictionary(g => g.Key, g => g.First());

        return idsList.ToDictionary(
            orgId => orgId,
            orgId =>
            {
                membershipByOrgId.TryGetValue(orgId, out var membership);
                return OrganizationMembership.ResolveEffectiveStatus(membership?.Status, globalStatus);
            });
    }

    private static List<string> IntersectObjectIds(IList<string> existing, IList<string> additional)
    {
        return existing == null ? additional.ToList() : existing.Intersect(additional).ToList();
    }
}

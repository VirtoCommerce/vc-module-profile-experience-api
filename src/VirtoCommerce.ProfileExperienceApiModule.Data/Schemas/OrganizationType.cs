using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
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
        IMediator mediator,
        IMemberAggregateFactory factory,
        IMemberService memberService,
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
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
            .ResolveAsync(async context => await ResolveMyStatusInOrganizationAsync(context, organizationMembershipService, memberService, userManagerFactory));

        var connectionBuilder = GraphTypeExtensionHelper.CreateConnection<ContactType, OrganizationAggregate>("contacts")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .Argument<ListGraphType<StringGraphType>>("roleIds", "Filter contacts by role IDs (org-level, membership, or global)")
            .Argument<ListGraphType<StringGraphType>>("statuses", "Filter contacts by effective status/lock state for this organization (e.g. Approved, Invited, Locked)")
            .PageSize(20);

        connectionBuilder.ResolveAsync(context => ResolveContactsConnectionAsync(
            context, mediator, factory, memberService, memberSearchService, organizationMembershipService, roleManagerFactory, userManagerFactory));
        AddField(connectionBuilder.FieldType);
    }

    private static async Task<object> ResolveContactsConnectionAsync(
        IResolveConnectionContext<OrganizationAggregate> context,
        IMediator mediator,
        IMemberAggregateFactory factory,
        IMemberService memberService,
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var query = context.GetSearchMembersQuery<SearchContactsQuery>();
        var orgId = context.Source.Organization.Id;
        query.MemberId = orgId;
        query.DeepSearch = false;

        var roleIds = context.GetArgument<IList<string>>("roleIds");
        if (roleIds is { Count: > 0 })
        {
            var (filterRequired, filterIds) = await ResolveRoleFilterAsync(
                orgId,
                roleIds,
                memberService,
                memberSearchService,
                organizationMembershipService,
                roleManagerFactory,
                userManagerFactory);

            if (filterRequired)
            {
                if (filterIds.Count == 0)
                {
                    return new PagedConnection<ContactAggregate>([], query.Skip, query.Take, 0);
                }

                query.ObjectIds = IntersectObjectIds(query.ObjectIds, filterIds);
            }
        }

        var statuses = context.GetArgument<IList<string>>("statuses");
        if (statuses is { Count: > 0 })
        {
            var (filterRequired, filterIds) = await ResolveStatusFilterAsync(
                orgId,
                statuses,
                memberSearchService,
                organizationMembershipService);

            if (filterRequired)
            {
                if (filterIds.Count == 0)
                {
                    return new PagedConnection<ContactAggregate>([], query.Skip, query.Take, 0);
                }

                query.ObjectIds = IntersectObjectIds(query.ObjectIds, filterIds);
            }
        }

        var response = await mediator.Send(query);

        return new PagedConnection<ContactAggregate>(
            response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take,
            response.TotalCount);
    }

    private static async Task<string> ResolveMyStatusInOrganizationAsync(
        IResolveFieldContext<OrganizationAggregate> context,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IMemberService memberService,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var userId = context.GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        using var userManager = userManagerFactory();
        var user = await userManager.FindByIdAsync(userId);
        if (string.IsNullOrEmpty(user?.MemberId))
        {
            return null;
        }

        var memberTask = memberService.GetByIdAsync(user.MemberId);
        var membershipTask = organizationMembershipSearchService.GetMembershipAsync(userId, context.Source.Organization.Id);

        await Task.WhenAll(memberTask, membershipTask);

        return OrganizationMembership.ResolveEffectiveStatus(membershipTask.Result?.Status, memberTask.Result?.Status);
    }

    private static async Task<IReadOnlyCollection<string>> GetContactIdsByGlobalRolesAsync(
        IList<string> roleIds,
        IReadOnlyCollection<ApplicationUser> orgUsers,
        RoleManager<Role> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        var requestedRoleNames = await roleManager.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync();

        if (requestedRoleNames.Count == 0)
        {
            return [];
        }

        var requestedRoleNameSet = requestedRoleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var user in orgUsers)
        {
            var userRoleNames = await userManager.GetRolesAsync(user);
            if (userRoleNames.Any(requestedRoleNameSet.Contains))
            {
                var contactId = user.MemberId ?? user.Id;
                if (!string.IsNullOrEmpty(contactId))
                {
                    result.Add(contactId);
                }
            }
        }

        return result;
    }

    private static async Task<(bool filterRequired, IReadOnlyCollection<string> ids)> ResolveRoleFilterAsync(
        string orgId,
        IList<string> roleIds,
        IMemberService memberService,
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var organization = await memberService.GetByIdAsync(orgId, memberType: nameof(Organization)) as Organization;
        var orgRoleIds = organization?.Roles?.Select(r => r.RoleId).ToHashSet() ?? [];

        if (roleIds.Any(orgRoleIds.Contains))
        {
            return (filterRequired: false, ids: []);
        }

        var membershipsTask = organizationMembershipService.SearchAllNoCloneAsync(
            new OrganizationMembershipSearchCriteria { OrganizationId = orgId });
        var contactsTask = memberSearchService.SearchAllAsync(
            new MembersSearchCriteria { MemberId = orgId });

        await Task.WhenAll(membershipsTask, contactsTask);

        var allOrgMemberships = membershipsTask.Result;
        var orgMembershipUserIds = allOrgMemberships.Select(m => m.UserId).ToHashSet();
        var orgContactIds = contactsTask.Result.Select(c => c.Id).ToHashSet();

        List<ApplicationUser> orgUsers;
        using (var um = userManagerFactory())
        {
            orgUsers = await um.Users
                .Where(u => orgMembershipUserIds.Contains(u.Id) ||
                            (!string.IsNullOrEmpty(u.MemberId) && orgContactIds.Contains(u.MemberId)))
                .ToListAsync();
        }

        if (orgUsers.Count == 0)
        {
            return (filterRequired: true, ids: []);
        }

        var membershipUserIds = allOrgMemberships
            .Where(m => m.Roles?.Any(r => roleIds.Contains(r.RoleId)) == true)
            .Select(m => m.UserId)
            .ToHashSet();

        using var globalRoleManager = roleManagerFactory();
        using var globalUserManager = userManagerFactory();
        var qualifyingContactIds = (await GetContactIdsByGlobalRolesAsync(roleIds, orgUsers, globalRoleManager, globalUserManager))
            .ToHashSet();

        qualifyingContactIds.UnionWith(
            orgUsers
                .Where(u => membershipUserIds.Contains(u.Id))
                .Select(u => u.MemberId ?? u.Id)
                .Where(id => !string.IsNullOrEmpty(id)));

        return (filterRequired: true, ids: qualifyingContactIds);
    }

    private const string LockedFilterValue = "Locked";

    private static async Task<(bool filterRequired, IReadOnlyCollection<string> ids)> ResolveStatusFilterAsync(
        string orgId,
        IList<string> statuses,
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService)
    {
        var membershipsTask = organizationMembershipService.SearchAllNoCloneAsync(
            new OrganizationMembershipSearchCriteria { OrganizationId = orgId });
        var contactsTask = memberSearchService.SearchAllAsync(
            new MembersSearchCriteria { MemberId = orgId });

        await Task.WhenAll(membershipsTask, contactsTask);

        var membershipByUserId = membershipsTask.Result
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var wantsLocked = statuses.Contains(LockedFilterValue);
        var lifecycleStatuses = statuses.Where(s => s != LockedFilterValue).ToHashSet();

        var qualifyingContactIds = contactsTask.Result
            .Where(contact => ContactMatchesStatusFilter(contact, membershipByUserId, wantsLocked, lifecycleStatuses))
            .Select(contact => contact.Id)
            .ToHashSet();

        return (filterRequired: true, ids: qualifyingContactIds);
    }

    private static bool ContactMatchesStatusFilter(
        Member contact, IDictionary<string, OrganizationMembership> membershipByUserId, bool wantsLocked, ISet<string> lifecycleStatuses)
    {
        var membership = FindMembership(contact, membershipByUserId);

        if (membership?.IsCurrentlyLocked == true)
        {
            return wantsLocked;
        }

        if (lifecycleStatuses.Count == 0)
        {
            return false;
        }

        var effectiveStatus = OrganizationMembership.ResolveEffectiveStatus(membership?.Status, contact.Status);

        return !string.IsNullOrEmpty(effectiveStatus) && lifecycleStatuses.Contains(effectiveStatus);
    }

    private static OrganizationMembership FindMembership(Member contact, IDictionary<string, OrganizationMembership> membershipByUserId)
    {
        var securityAccountIds = (contact as IHasSecurityAccounts)?.SecurityAccounts?
            .Select(sa => sa.Id)
            .Where(id => !string.IsNullOrEmpty(id)) ?? [];

        foreach (var securityAccountId in securityAccountIds)
        {
            if (membershipByUserId.TryGetValue(securityAccountId, out var membership))
            {
                return membership;
            }
        }

        return null;
    }

    private static IList<string> IntersectObjectIds(IList<string> existing, IReadOnlyCollection<string> additional)
    {
        return existing == null ? additional.ToList() : existing.Intersect(additional).ToList();
    }
}

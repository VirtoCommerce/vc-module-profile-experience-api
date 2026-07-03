using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        #region Contacts

        var connectionBuilder = GraphTypeExtensionHelper.CreateConnection<ContactType, OrganizationAggregate>("contacts")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .Argument<ListGraphType<StringGraphType>>("roleIds", "Filter contacts by role IDs (org-level, membership, or global)")
            .PageSize(20);

        connectionBuilder.ResolveAsync(async context =>
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

                    query.ObjectIds = filterIds.ToList();
                }
            }

            var response = await mediator.Send(query);

            return new PagedConnection<ContactAggregate>(
                response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take,
                response.TotalCount);
        });
        AddField(connectionBuilder.FieldType);

        #endregion
    }

    private static async Task<IEnumerable<string>> GetContactIdsByGlobalRoleAsync(
        string roleId,
        IReadOnlyCollection<ApplicationUser> orgUsers,
        RoleManager<Role> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return [];
        }

        var result = new List<string>();

        foreach (var user in orgUsers)
        {
            if (await userManager.IsInRoleAsync(user, role.NormalizedName))
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

        // Each task creates its own RoleManager/UserManager — EF Core DbContext is not
        // thread-safe for concurrent operations on a shared instance.
        var globalResults = await Task.WhenAll(
            roleIds.Select(async roleId =>
            {
                using var roleManager = roleManagerFactory();
                using var userManager = userManagerFactory();
                return await GetContactIdsByGlobalRoleAsync(roleId, orgUsers, roleManager, userManager);
            }));

        var qualifyingContactIds = globalResults.SelectMany(x => x).ToHashSet();

        qualifyingContactIds.UnionWith(
            orgUsers
                .Where(u => membershipUserIds.Contains(u.Id))
                .Select(u => u.MemberId ?? u.Id)
                .Where(id => !string.IsNullOrEmpty(id)));

        return (filterRequired: true, ids: qualifyingContactIds);
    }
}

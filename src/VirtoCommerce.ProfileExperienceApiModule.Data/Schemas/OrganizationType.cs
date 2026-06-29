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
using VirtoCommerce.CustomerModule.Core.Services;
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
        IOrganizationMembershipService organizationMembershipService,
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
        RoleManager<Role> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return [];
        }

        var usersInRole = await userManager.GetUsersInRoleAsync(role.NormalizedName);

        return usersInRole.Where(u => !string.IsNullOrEmpty(u.MemberId)).Select(u => u.MemberId);
    }

    private static async Task<(bool filterRequired, IReadOnlyCollection<string> ids)> ResolveRoleFilterAsync(
        string orgId,
        IList<string> roleIds,
        IMemberService memberService,
        IOrganizationMembershipService organizationMembershipService,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var organization = await memberService.GetByIdAsync(orgId, memberType: nameof(Organization)) as Organization;
        var orgRoleIds = organization?.Roles?.Select(r => r.RoleId).ToHashSet() ?? [];

        if (roleIds.Any(orgRoleIds.Contains))
        {
            return (filterRequired: false, ids: []);
        }

        using var roleManager = roleManagerFactory();
        using var userManager = userManagerFactory();

        var membershipTask = organizationMembershipService.GetUserIdsByRoleInOrgAsync(orgId, roleIds);
        var globalResults = await Task.WhenAll(
            roleIds.Select(roleId => GetContactIdsByGlobalRoleAsync(roleId, roleManager, userManager)));

        var qualifyingContactIds = globalResults.SelectMany(result => result).ToHashSet();

        var membershipUserIds = await membershipTask;
        if (membershipUserIds.Count > 0)
        {
            var memberContactIds = await userManager.Users
                .Where(u => membershipUserIds.Contains(u.Id) && !string.IsNullOrEmpty(u.MemberId))
                .Select(u => u.MemberId)
                .ToListAsync();
            qualifyingContactIds.UnionWith(memberContactIds);
        }

        return (filterRequired: true, ids: qualifyingContactIds);
    }
}

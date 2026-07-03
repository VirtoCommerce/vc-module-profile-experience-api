using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Identity;
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
        Func<RoleManager<Role>> roleManagerFactory,
        ICustomerPreferenceService customerPreferenceService,
        IMediator mediator,
        IMemberAggregateFactory memberAggregateFactory,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IDataLoaderContextAccessor dataLoader)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        _userManagerFactory = userManagerFactory;
        _customerPreferenceService = customerPreferenceService;

        Field<BooleanGraphType>("isLockedInOrganization")
            .Resolve(context => ResolveIsLockedInOrganization(context, organizationMembershipSearchService, dataLoader));

        Field<ListGraphType<RoleType>>("rolesInOrganization")
            .Resolve(context => ResolveRolesInOrganization(
                context, organizationMembershipSearchService, dataLoader, roleManagerFactory, userManagerFactory));

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

        ExtendableFieldAsync<OrganizationType>("organization", resolve: async context =>
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

    private static IDataLoaderResult<bool> ResolveIsLockedInOrganization(
        IResolveFieldContext<ContactAggregate> context,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IDataLoaderContextAccessor dataLoader)
    {
        var organizationId = context.GetCurrentOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
        {
            return new DataLoaderResult<bool>(false);
        }

        var userIds = GetSecurityAccountIds(context);
        if (userIds.Count == 0)
        {
            return new DataLoaderResult<bool>(false);
        }

        // User ids without a locked membership are missing from the dictionary and resolve to the default (false)
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, bool>(
            $"contact_lockedInOrg_{organizationId}",
            async ids =>
            {
                var idsList = ids.ToList();
                var lockedMemberships = await organizationMembershipSearchService.SearchAllNoCloneAsync(
                    new OrganizationMembershipSearchCriteria
                    {
                        OrganizationId = organizationId,
                        UserIds = idsList,
                        OnlyLocked = true,
                        Take = idsList.Count,
                    });

                return (IDictionary<string, bool>)lockedMemberships
                    .GroupBy(m => m.UserId)
                    .ToDictionary(g => g.Key, _ => true);
            });

        return loader.LoadAsync(userIds).Then(lockedFlags => lockedFlags.Any(locked => locked));
    }

    private static IDataLoaderResult<List<Role>> ResolveRolesInOrganization(
        IResolveFieldContext<ContactAggregate> context,
        IOrganizationMembershipSearchService organizationMembershipSearchService,
        IDataLoaderContextAccessor dataLoader,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var organizationId = context.GetCurrentOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
        {
            return new DataLoaderResult<List<Role>>((List<Role>)null);
        }

        var userIds = GetSecurityAccountIds(context);
        if (userIds.Count == 0)
        {
            return new DataLoaderResult<List<Role>>((List<Role>)null);
        }

        // The loader is keyed by user id (finer than the contact), so the per-contact union happens in Then
        var loader = dataLoader.Context.GetOrAddBatchLoader<string, IReadOnlyCollection<Role>>(
            $"contact_rolesInOrg_{organizationId}",
            async ids =>
            {
                var idsList = ids.ToList();

                var membershipRolesTask = organizationMembershipSearchService.GetRolesForUsersInOrgAsync(idsList, organizationId);
                var globalRolesTask = GetGlobalRolesByUserAsync(idsList, roleManagerFactory, userManagerFactory);

                await Task.WhenAll(membershipRolesTask, globalRolesTask);

                var membershipRoles = membershipRolesTask.Result;
                var globalRoles = globalRolesTask.Result;

                return (IDictionary<string, IReadOnlyCollection<Role>>)idsList.ToDictionary(
                    id => id,
                    id =>
                    {
                        var orgRoles = membershipRoles.TryGetValue(id, out var roles)
                            ? roles.Select(r => new Role { Id = r.RoleId, Name = r.RoleName })
                            : [];
                        var userGlobalRoles = globalRoles.TryGetValue(id, out var gRoles) ? gRoles : [];

                        return (IReadOnlyCollection<Role>)orgRoles
                            .Concat(userGlobalRoles)
                            .DistinctBy(r => r.Id)
                            .ToList();
                    });
            });

        return loader.LoadAsync(userIds).Then(rolesPerAccount =>
        {
            var roles = rolesPerAccount
                .Where(accountRoles => accountRoles != null)
                .SelectMany(accountRoles => accountRoles)
                .DistinctBy(r => r.Id)
                .ToList();

            return roles.Count > 0 ? roles : null;
        });
    }

    // Mirrors OrganizationType's global-role check: a user can hold an ASP.NET Identity role
    // that is not tied to any OrganizationMembership or org-level role assignment
    private static async Task<IDictionary<string, IReadOnlyCollection<Role>>> GetGlobalRolesByUserAsync(
        IList<string> userIds,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        using var roleManager = roleManagerFactory();
        using var userManager = userManagerFactory();

        // The role set is small — load it once instead of issuing a filtered query per user
        var rolesByName = roleManager.Roles.ToLookup(r => r.Name);

        var result = new Dictionary<string, IReadOnlyCollection<Role>>();

        foreach (var userId in userIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                continue;
            }

            var roleNames = await userManager.GetRolesAsync(user);
            if (roleNames.Count == 0)
            {
                continue;
            }

            result[userId] = roleNames
                .SelectMany(roleName => rolesByName[roleName])
                .Select(r => new Role { Id = r.Id, Name = r.Name })
                .ToList();
        }

        return result;
    }

    private static List<string> GetSecurityAccountIds(IResolveFieldContext<ContactAggregate> context) =>
        context.Source.Contact.SecurityAccounts?
            .Select(sa => sa.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList() ?? [];

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

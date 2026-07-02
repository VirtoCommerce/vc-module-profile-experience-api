using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Execution;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Schemas
{
    public class ContactTypeTests
    {
        private const string OrgId = "org-1";

        private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<Role>> _roleManagerMock;
        private readonly ContactType _contactType;

        public ContactTypeTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _roleManagerMock = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);

            _roleManagerMock.Setup(x => x.Roles).Returns(Enumerable.Empty<Role>().AsQueryable());

            _contactType = new ContactType(
                new Mock<IStoreService>().Object,
                new Mock<IDynamicPropertyResolverService>().Object,
                new Mock<IMemberAddressService>().Object,
                () => _userManagerMock.Object,
                () => _roleManagerMock.Object,
                new Mock<ICustomerPreferenceService>().Object,
                new Mock<IMediator>().Object,
                new Mock<IMemberAggregateFactory>().Object,
                _membershipSearchServiceMock.Object,
                new DataLoaderContextAccessor { Context = new DataLoaderContext() });
        }

        [Fact]
        public async Task RolesInOrganization_PageOfContacts_FetchesRolesInSingleBatch()
        {
            // Arrange
            _membershipSearchServiceMock
                .Setup(x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), OrgId))
                .ReturnsAsync((IList<string> ids, string _) => ids.ToDictionary(
                    id => id,
                    id => (IReadOnlyCollection<OrganizationRole>)[new OrganizationRole { RoleId = "r1", RoleName = "Admin" }]));

            // Act — resolve the field for a page of 3 contacts, then await the results (triggers the batch)
            var results = await ResolveForContactsAsync("rolesInOrganization", count: 3);

            // Assert — one fetch with the union of all user ids, not one per contact
            _membershipSearchServiceMock.Verify(
                x => x.GetRolesForUsersInOrgAsync(
                    It.Is<IList<string>>(ids =>
                        ids.Count == 3 && ids.Contains("user-1") && ids.Contains("user-2") && ids.Contains("user-3")),
                    OrgId),
                Times.Once);

            Assert.All(results, result =>
            {
                var roles = Assert.IsType<IEnumerable<Role>>(result, exactMatch: false);
                Assert.Equal("r1", roles.Single().Id);
            });
        }

        [Fact]
        public async Task RolesInOrganization_UserWithOnlyGlobalRole_IncludesGlobalRole()
        {
            // Arrange — user-1 has no membership/org-level role but holds a global Identity role
            _membershipSearchServiceMock
                .Setup(x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), OrgId))
                .ReturnsAsync((IList<string> ids, string _) => ids.ToDictionary(
                    id => id,
                    id => (IReadOnlyCollection<OrganizationRole>)[]));

            var globalUser = new ApplicationUser { Id = "user-1" };
            _userManagerMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(globalUser);
            _userManagerMock.Setup(x => x.GetRolesAsync(globalUser)).ReturnsAsync(["CSR"]);
            _roleManagerMock.Setup(x => x.Roles).Returns(new[] { new Role { Id = "csr-role-id", Name = "CSR" } }.AsQueryable());

            // Act
            var results = await ResolveForContactsAsync("rolesInOrganization", count: 1);

            // Assert
            var roles = Assert.IsType<IEnumerable<Role>>(results.Single(), exactMatch: false);
            Assert.Equal("csr-role-id", roles.Single().Id);
        }

        [Fact]
        public async Task RolesInOrganization_ArgumentOrganizationIdDiffersFromCallerOrg_UsesCallersOrganization()
        {
            // Arrange — the caller's session is scoped to OrgId, but the GraphQL argument requests a different org
            const string otherOrgId = "org-attacker-target";
            _membershipSearchServiceMock
                .Setup(x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), OrgId))
                .ReturnsAsync((IList<string> ids, string _) => ids.ToDictionary(
                    id => id,
                    id => (IReadOnlyCollection<OrganizationRole>)[new OrganizationRole { RoleId = "r1", RoleName = "Admin" }]));

            var context = BuildContext(contactId: "contact-1", userId: "user-1", argumentOrganizationId: otherOrgId, callerOrganizationId: OrgId);
            await ResolveFieldAsync("rolesInOrganization", context);

            // Assert — the query used the caller's own organization, never the argument's organization
            _membershipSearchServiceMock.Verify(
                x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), OrgId),
                Times.Once);
            _membershipSearchServiceMock.Verify(
                x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), otherOrgId),
                Times.Never);
        }

        [Fact]
        public async Task RolesInOrganization_CallerHasNoCurrentOrganization_ReturnsNull()
        {
            // Arrange — no organization_id claim on the caller (e.g. anonymous or org-less user)
            var context = BuildContext(contactId: "contact-1", userId: "user-1", callerOrganizationId: null);

            // Act
            var result = await ResolveFieldAsync("rolesInOrganization", context);

            // Assert
            Assert.Null(result);
            _membershipSearchServiceMock.Verify(
                x => x.GetRolesForUsersInOrgAsync(It.IsAny<IList<string>>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task IsLockedInOrganization_PageOfContacts_FetchesLocksInSingleBatch()
        {
            // Arrange — only user-2 has a locked membership; missing keys must resolve to false
            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.OrganizationId == OrgId && c.OnlyLocked),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = [new OrganizationMembership { UserId = "user-2", OrganizationId = OrgId }],
                    TotalCount = 1,
                });

            // Act
            var results = await ResolveForContactsAsync("isLockedInOrganization", count: 3);

            // Assert — one search for the whole page
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c =>
                        c.OrganizationId == OrgId &&
                        c.OnlyLocked &&
                        c.UserIds.Count == 3),
                    It.IsAny<bool>()),
                Times.Once);

            Assert.Equal([false, true, false], results.Cast<bool>());
        }

        [Fact]
        public async Task IsLockedInOrganization_ArgumentOrganizationIdDiffersFromCallerOrg_UsesCallersOrganization()
        {
            // Arrange — the caller's session is scoped to OrgId, but the GraphQL argument requests a different org
            const string otherOrgId = "org-attacker-target";
            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [], TotalCount = 0 });

            var context = BuildContext(contactId: "contact-1", userId: "user-1", argumentOrganizationId: otherOrgId, callerOrganizationId: OrgId);
            await ResolveFieldAsync("isLockedInOrganization", context);

            // Assert — the query used the caller's own organization, never the argument's organization
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.Is<OrganizationMembershipSearchCriteria>(c => c.OrganizationId == OrgId), It.IsAny<bool>()),
                Times.Once);
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.Is<OrganizationMembershipSearchCriteria>(c => c.OrganizationId == otherOrgId), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task IsLockedInOrganization_CallerHasNoCurrentOrganization_ReturnsFalse()
        {
            // Arrange — no organization_id claim on the caller (e.g. anonymous or org-less user)
            var context = BuildContext(contactId: "contact-1", userId: "user-1", callerOrganizationId: null);

            // Act
            var result = await ResolveFieldAsync("isLockedInOrganization", context);

            // Assert
            Assert.Equal(false, result);
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Never);
        }

        private async Task<List<object>> ResolveForContactsAsync(string fieldName, int count)
        {
            var field = _contactType.Fields.First(f => f.Name == fieldName);

            // Resolve all contacts first — the loader accumulates keys across the page
            var pendingResults = new List<object>();
            for (var i = 1; i <= count; i++)
            {
                var context = BuildContext(contactId: $"contact-{i}", userId: $"user-{i}");
                pendingResults.Add(await field.Resolver.ResolveAsync(context));
            }

            // Awaiting dispatches a single batched fetch for all accumulated keys
            var results = new List<object>();
            foreach (var pendingResult in pendingResults)
            {
                results.Add(await UnwrapAsync(pendingResult));
            }

            return results;
        }

        private async Task<object> ResolveFieldAsync(string fieldName, ResolveFieldContext<ContactAggregate> context)
        {
            var field = _contactType.Fields.First(f => f.Name == fieldName);
            var pendingResult = await field.Resolver.ResolveAsync(context);

            return await UnwrapAsync(pendingResult);
        }

        private static async Task<object> UnwrapAsync(object pendingResult)
        {
            var dataLoaderResult = Assert.IsType<IDataLoaderResult>(pendingResult, exactMatch: false);

            return await dataLoaderResult.GetResultAsync();
        }

        private static ResolveFieldContext<ContactAggregate> BuildContext(
            string contactId,
            string userId,
            string argumentOrganizationId = OrgId,
            string callerOrganizationId = OrgId) =>
            new()
            {
                Source = new ContactAggregate
                {
                    Member = new Contact
                    {
                        Id = contactId,
                        SecurityAccounts = [new ApplicationUser { Id = userId }],
                    },
                },
                Arguments = new Dictionary<string, ArgumentValue>
                {
                    ["organizationId"] = new ArgumentValue(argumentOrganizationId, ArgumentSource.Literal),
                },
                UserContext = new GraphQLUserContext(BuildPrincipal(callerOrganizationId)),
            };

        private static ClaimsPrincipal BuildPrincipal(string organizationId)
        {
            var claims = string.IsNullOrEmpty(organizationId)
                ? []
                : new[] { new Claim(ModuleConstants.Security.Claims.OrganizationId, organizationId) };

            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.DataLoader;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Schemas
{
    public class OrganizationTypeTests
    {
        private const string UserId = "user-1";
        private const string MemberId = "member-1";

        private readonly Mock<IMemberService> _memberServiceMock = new();
        private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly OrganizationType _organizationType;

        public OrganizationTypeTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
            _userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = MemberId });

            var roleManagerMock = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);

            _organizationType = new OrganizationType(
                new Mock<IStoreService>().Object,
                new Mock<IDynamicPropertyResolverService>().Object,
                new Mock<IMemberAddressService>().Object,
                new Mock<IMediator>().Object,
                new Mock<IMemberAggregateFactory>().Object,
                _memberServiceMock.Object,
                new Mock<IMemberSearchService>().Object,
                _membershipSearchServiceMock.Object,
                () => roleManagerMock.Object,
                () => _userManagerMock.Object,
                new DataLoaderContextAccessor { Context = new DataLoaderContext() });
        }

        [Fact]
        public async Task MyStatusInOrganization_PageOfOrganizations_FetchesUserAndMembershipsInSingleBatch()
        {
            // Arrange — user has an Approved override in org-2 only; org-1 and org-3 fall back to the
            // contact's global status.
            _memberServiceMock
                .Setup(x => x.GetByIdAsync(MemberId, null, null))
                .ReturnsAsync(new Contact { Id = MemberId, Status = ModuleConstants.MembershipStatuses.Rejected });

            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == UserId),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = [new OrganizationMembership { UserId = UserId, OrganizationId = "org-2", Status = ModuleConstants.MembershipStatuses.Approved }],
                    TotalCount = 1,
                });

            // Act — resolve the field for a page of 3 organizations, then await (triggers the batch)
            var results = await ResolveForOrganizationsAsync(["org-1", "org-2", "org-3"]);

            // Assert — a single membership search for the whole page, not one per organization
            _memberServiceMock.Verify(x => x.GetByIdAsync(MemberId, null, null), Times.Once);
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c =>
                        c.UserId == UserId && c.OrganizationIds.Count == 3),
                    It.IsAny<bool>()),
                Times.Once);

            Assert.Equal(
                [ModuleConstants.MembershipStatuses.Rejected, ModuleConstants.MembershipStatuses.Approved, ModuleConstants.MembershipStatuses.Rejected],
                results);
        }

        [Fact]
        public async Task MyStatusInOrganization_NoCurrentUser_ReturnsNull_WithoutQuerying()
        {
            // Arrange — no organization_id/user claim on the caller (e.g. anonymous)
            var context = BuildContext("org-1", userId: null);

            // Act
            var result = await ResolveFieldAsync(context);

            // Assert
            Assert.Null(result);
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Never);
        }

        private async Task<List<object>> ResolveForOrganizationsAsync(IList<string> organizationIds)
        {
            var field = _organizationType.Fields.First(f => f.Name == "myStatusInOrganization");

            var pendingResults = new List<object>();
            foreach (var orgId in organizationIds)
            {
                pendingResults.Add(await field.Resolver.ResolveAsync(BuildContext(orgId, UserId)));
            }

            var results = new List<object>();
            foreach (var pendingResult in pendingResults)
            {
                results.Add(await UnwrapAsync(pendingResult));
            }

            return results;
        }

        private async Task<object> ResolveFieldAsync(ResolveFieldContext<OrganizationAggregate> context)
        {
            var field = _organizationType.Fields.First(f => f.Name == "myStatusInOrganization");
            var pendingResult = await field.Resolver.ResolveAsync(context);

            return await UnwrapAsync(pendingResult);
        }

        private static async Task<object> UnwrapAsync(object pendingResult)
        {
            var dataLoaderResult = Assert.IsType<IDataLoaderResult>(pendingResult, exactMatch: false);

            return await dataLoaderResult.GetResultAsync();
        }

        private static ResolveFieldContext<OrganizationAggregate> BuildContext(string organizationId, string userId) =>
            new()
            {
                Source = new OrganizationAggregate { Member = new Organization { Id = organizationId } },
                UserContext = new GraphQLUserContext(BuildPrincipal(userId)),
            };

        private static ClaimsPrincipal BuildPrincipal(string userId)
        {
            var claims = string.IsNullOrEmpty(userId)
                ? []
                : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };

            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
    }
}

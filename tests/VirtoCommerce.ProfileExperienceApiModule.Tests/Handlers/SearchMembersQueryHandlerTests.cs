using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class SearchMembersQueryHandlerTests : MoqHelper
    {
        private readonly Mock<IMemberSearchService> _memberSearchServiceMock = new();
        private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();

        [Fact]
        public async Task Handle_SearchOrganizations_WithoutUserId_ReturnsAllResults()
        {
            // Arrange
            var organizations = new List<Member>
            {
                new Organization { Id = "org-1" },
                new Organization { Id = "org-2" },
            };

            _memberSearchServiceMock
                .Setup(x => x.SearchMembersAsync(It.IsAny<MembersSearchCriteria>()))
                .ReturnsAsync(new MemberSearchResult { Results = organizations, TotalCount = 2 });

            var handler = BuildHandler();
            var query = new SearchOrganizationsQuery { UserId = null };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TotalCount);
            _membershipSearchServiceMock.Verify(
                x => x.GetLockedOrganizationIdsAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Handle_SearchOrganizations_ExcludesLockedOrgsBeforeSearch()
        {
            // Arrange — locked orgs are excluded via search criteria (before pagination), not post-filtered
            _memberSearchServiceMock
                .Setup(x => x.SearchMembersAsync(It.IsAny<MembersSearchCriteria>()))
                .ReturnsAsync(new MemberSearchResult { Results = [], TotalCount = 0 });

            _membershipSearchServiceMock
                .Setup(x => x.GetLockedOrganizationIdsAsync("user-1"))
                .ReturnsAsync(["org-2"]);

            var handler = BuildHandler();
            var query = new SearchOrganizationsQuery { UserId = "user-1" };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert
            _memberSearchServiceMock.Verify(
                x => x.SearchMembersAsync(It.Is<MembersSearchCriteria>(c =>
                    c.ExcludedObjectIds != null && c.ExcludedObjectIds.Contains("org-2"))),
                Times.Once);
        }

        [Fact]
        public async Task Handle_SearchOrganizations_NoLockedOrgs_DoesNotSetExclusion()
        {
            // Arrange
            _memberSearchServiceMock
                .Setup(x => x.SearchMembersAsync(It.IsAny<MembersSearchCriteria>()))
                .ReturnsAsync(new MemberSearchResult { Results = [], TotalCount = 0 });

            _membershipSearchServiceMock
                .Setup(x => x.GetLockedOrganizationIdsAsync("user-1"))
                .ReturnsAsync([]);

            var handler = BuildHandler();
            var query = new SearchOrganizationsQuery { UserId = "user-1" };

            // Act
            await handler.Handle(query, CancellationToken.None);

            // Assert
            _memberSearchServiceMock.Verify(
                x => x.SearchMembersAsync(It.Is<MembersSearchCriteria>(c => c.ExcludedObjectIds.IsNullOrEmpty())),
                Times.Once);
        }

        private SearchMembersQueryHandler BuildHandler() =>
            new(_memberSearchServiceMock.Object, _membershipSearchServiceMock.Object);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class SearchMembersQueryHandlerTests : MoqHelper
    {
        private readonly Mock<IMemberSearchService> _memberSearchServiceMock = new();
        private readonly Mock<IOrganizationMembershipService> _membershipServiceMock = new();

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
            _membershipServiceMock.Verify(x => x.GetLockedOrganizationIdsAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SearchOrganizations_FiltersLockedOrgs()
        {
            // Arrange
            var organizations = new List<Member>
            {
                new Organization { Id = "org-1" },
                new Organization { Id = "org-2" },
                new Organization { Id = "org-3" },
            };

            _memberSearchServiceMock
                .Setup(x => x.SearchMembersAsync(It.IsAny<MembersSearchCriteria>()))
                .ReturnsAsync(new MemberSearchResult { Results = organizations, TotalCount = 3 });

            _membershipServiceMock
                .Setup(x => x.GetLockedOrganizationIdsAsync("user-1"))
                .ReturnsAsync(["org-2"]);

            var handler = BuildHandler();
            var query = new SearchOrganizationsQuery { UserId = "user-1" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.DoesNotContain(result.Results, r => r.Id == "org-2");
        }

        [Fact]
        public async Task Handle_SearchOrganizations_NoLockedOrgs_ReturnsUnchangedResults()
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

            _membershipServiceMock
                .Setup(x => x.GetLockedOrganizationIdsAsync("user-1"))
                .ReturnsAsync([]);

            var handler = BuildHandler();
            var query = new SearchOrganizationsQuery { UserId = "user-1" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Results.Count);
        }

        private SearchMembersQueryHandler BuildHandler() =>
            new(_memberSearchServiceMock.Object, _membershipServiceMock.Object);
    }
}

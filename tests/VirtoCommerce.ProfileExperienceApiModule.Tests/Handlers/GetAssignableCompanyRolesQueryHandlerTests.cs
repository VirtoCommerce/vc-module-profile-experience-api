using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class GetAssignableCompanyRolesQueryHandlerTests : MoqHelper
    {
        private readonly Mock<ICompanyMemberRoleService> _companyMemberRoleServiceMock = new();
        private readonly Mock<RoleManager<Role>> _roleManagerMock;

        public GetAssignableCompanyRolesQueryHandlerTests()
        {
            _roleManagerMock = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);
        }

        [Fact]
        public async Task Handle_WhitelistEntryDoesNotMatchAnyRealRole_IsExcluded()
        {
            // Arrange - whitelist has a typo ("test") alongside a real role
            _companyMemberRoleServiceMock
                .Setup(x => x.GetAllowedRoleIdsAsync("store-1"))
                .ReturnsAsync(["test", "org-employee"]);

            var platformRoles = new List<Role>
            {
                new() { Id = "org-employee", Name = "Organization employee" },
                new() { Id = "org-maintainer", Name = "Organization maintainer" },
            }.AsQueryable();

            _roleManagerMock.Setup(x => x.Roles).Returns(platformRoles);

            var handler = new GetAssignableCompanyRolesQueryHandler(_companyMemberRoleServiceMock.Object, () => _roleManagerMock.Object);
            var query = new GetAssignableCompanyRolesQuery { StoreId = "store-1" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert - only the whitelist entry that matches a real role is returned
            var role = Assert.Single(result);
            Assert.Equal("org-employee", role.Id);
        }

        [Fact]
        public async Task Handle_WhitelistMatchesMultipleRealRoles_ReturnsAllOfThem()
        {
            // Arrange
            _companyMemberRoleServiceMock
                .Setup(x => x.GetAllowedRoleIdsAsync("store-1"))
                .ReturnsAsync(["org-employee", "org-maintainer"]);

            var platformRoles = new List<Role>
            {
                new() { Id = "org-employee", Name = "Organization employee" },
                new() { Id = "org-maintainer", Name = "Organization maintainer" },
                new() { Id = "store-admin", Name = "Store administrator" },
            }.AsQueryable();

            _roleManagerMock.Setup(x => x.Roles).Returns(platformRoles);

            var handler = new GetAssignableCompanyRolesQueryHandler(_companyMemberRoleServiceMock.Object, () => _roleManagerMock.Object);
            var query = new GetAssignableCompanyRolesQuery { StoreId = "store-1" };

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == "org-employee");
            Assert.Contains(result, r => r.Id == "org-maintainer");
            Assert.DoesNotContain(result, r => r.Id == "store-admin");
        }
    }
}

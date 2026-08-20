using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class ResolveOrganizationRoleFilterQueryHandlerTests
{
    private const string OrgId = "org-1";

    private readonly Mock<IMemberService> _memberServiceMock = new();
    private readonly Mock<IMemberSearchService> _memberSearchServiceMock = new();
    private readonly Mock<IOrganizationMembershipSearchService> _organizationMembershipServiceMock = new();
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

    public ResolveOrganizationRoleFilterQueryHandlerTests()
    {
        _roleManagerMock = new Mock<RoleManager<Role>>(
            new Mock<IRoleStore<Role>>().Object, null, null, null, null);
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
    }

    private ResolveOrganizationRoleFilterQueryHandler BuildHandler() => new(
        _memberServiceMock.Object,
        _memberSearchServiceMock.Object,
        _organizationMembershipServiceMock.Object,
        () => _roleManagerMock.Object,
        () => _userManagerMock.Object);

    [Fact]
    public async Task Handle_RequestedRoleIsAnOrganizationLevelRole_SkipsFilteringEntirely()
    {
        // Arrange - the org itself has this role assigned (CustomerOrganizationRole), so every member
        // qualifies and the (expensive) per-member role resolution can be skipped entirely.
        _memberServiceMock
            .Setup(x => x.GetByIdAsync(OrgId, null, nameof(Organization)))
            .ReturnsAsync(new Organization
            {
                Id = OrgId,
                Roles = [new OrganizationRole { RoleId = "org-role-1", RoleName = "Warranty Specialist" }],
            });

        var handler = BuildHandler();
        var query = new ResolveOrganizationRoleFilterQuery { OrganizationId = OrgId, RoleIds = ["Warranty Specialist"] };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - no filtering needed, and no membership/user search was ever attempted
        Assert.False(result.FilterRequired);
        _organizationMembershipServiceMock.Verify(
            x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RequestedRoleMatchesOrgLevelRoleId_CaseInsensitive()
    {
        // Arrange - same as above but matching by Id (not Name) and differing only in case
        _memberServiceMock
            .Setup(x => x.GetByIdAsync(OrgId, null, nameof(Organization)))
            .ReturnsAsync(new Organization
            {
                Id = OrgId,
                Roles = [new OrganizationRole { RoleId = "Org-Role-1", RoleName = "Warranty Specialist" }],
            });

        var handler = BuildHandler();
        var query = new ResolveOrganizationRoleFilterQuery { OrganizationId = OrgId, RoleIds = ["org-role-1"] };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.FilterRequired);
    }
}

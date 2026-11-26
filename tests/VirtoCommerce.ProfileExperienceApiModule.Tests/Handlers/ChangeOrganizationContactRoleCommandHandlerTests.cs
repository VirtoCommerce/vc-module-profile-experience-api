using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class ChangeOrganizationContactRoleCommandHandlerTests
{
    private const string _userId = "user-1";

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;
    private readonly FrontendSecurityOptions _frontendSecurityOptions = new() { MembershipRolesWhitelist = ["role-1"] };

    public ChangeOrganizationContactRoleCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(_userId))
            .ReturnsAsync(new ApplicationUser { Id = _userId, UserName = "test@test.com" });

        _userManagerMock
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock = new Mock<RoleManager<Role>>(
            new Mock<IRoleStore<Role>>().Object, null, null, null, null);
    }

    [Fact]
    public async Task Handle_RoleNotInWhitelist_ReturnsRoleNotAllowedError()
    {
        // Arrange
        _roleManagerMock
            .Setup(x => x.FindByIdAsync("store-manager"))
            .ReturnsAsync(new Role { Id = "store-manager", Name = "Store manager", NormalizedName = "STORE MANAGER" });

        var command = new ChangeOrganizationContactRoleCommand
        {
            UserId = _userId,
            RoleIds = ["store-manager"],
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "RoleNotAllowed" && e.Parameter == "store-manager");
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhitelistedRole_UpdatesUserRoles()
    {
        // Arrange
        _roleManagerMock
            .Setup(x => x.FindByIdAsync("role-1"))
            .ReturnsAsync(new Role { Id = "role-1", Name = "TestRole", NormalizedName = "TESTROLE" });

        var command = new ChangeOrganizationContactRoleCommand
        {
            UserId = _userId,
            RoleIds = ["role-1"],
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Succeeded);
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(user => user.Roles.Count == 1 && user.Roles[0].Id == "role-1")), Times.Once);
    }

    private ChangeOrganizationContactRoleCommandHandler BuildHandler()
    {
        var userManager = _userManagerMock.Object;
        var roleManager = _roleManagerMock.Object;

        return new ChangeOrganizationContactRoleCommandHandler(
            () => userManager,
            () => roleManager,
            Options.Create(new AuthorizationOptions()),
            Options.Create(_frontendSecurityOptions));
    }
}

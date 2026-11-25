using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class InviteUserCommandHandlerTests
{
    private readonly Mock<IMemberService> _memberServiceMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<RoleManager<Role>> _roleManagerMock;

    public InviteUserCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed());

        _userManagerMock
            .Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _roleManagerMock = new Mock<RoleManager<Role>>(
            new Mock<IRoleStore<Role>>().Object, null, null, null, null);
    }

    [Fact]
    public async Task Handle_OrganizationInviteWithRoleNotInWhitelist_ReturnsRoleNotAllowedWithoutCreatingAccount()
    {
        // Arrange
        _roleManagerMock
            .Setup(x => x.FindByIdAsync("store-manager"))
            .ReturnsAsync(new Role { Id = "store-manager", Name = "Store manager" });

        var command = new InviteUserCommand
        {
            StoreId = "store-1",
            OrganizationId = "org-1",
            Emails = ["new@example.com"],
            RoleIds = ["store-manager"],
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "RoleNotAllowed" && e.Parameter == "store-manager");
        _memberServiceMock.Verify(x => x.SaveChangesAsync(It.IsAny<Member[]>()), Times.Never);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InviteWithoutOrganizationWithRoleNotInWhitelist_ReturnsRoleNotAllowedWithoutCreatingAccount()
    {
        // Arrange
        _roleManagerMock
            .Setup(x => x.FindByIdAsync("store-manager"))
            .ReturnsAsync(new Role { Id = "store-manager", Name = "Store manager" });

        var command = new InviteUserCommand
        {
            StoreId = "store-1",
            Emails = ["new@example.com"],
            RoleIds = ["store-manager"],
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == "RoleNotAllowed" && e.Parameter == "store-manager");
        _memberServiceMock.Verify(x => x.SaveChangesAsync(It.IsAny<Member[]>()), Times.Never);
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrganizationInviteWithDefaultWhitelistedRole_CreatesAccount()
    {
        // Arrange
        _roleManagerMock
            .Setup(x => x.FindByIdAsync("org-employee"))
            .ReturnsAsync(new Role { Id = "org-employee", Name = "Organization employee" });

        var command = new InviteUserCommand
        {
            StoreId = "store-1",
            OrganizationId = "org-1",
            Emails = ["new@example.com"],
            RoleIds = ["org-employee"],
        };

        var handler = BuildHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.Code == "RoleNotAllowed");
        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>()), Times.Once);
    }

    private InviteUserCommandHandler BuildHandler()
    {
        var userManager = _userManagerMock.Object;
        var roleManager = _roleManagerMock.Object;

        return new InviteUserCommandHandler(
            new Mock<IWebHostEnvironment>().Object,
            () => userManager,
            _memberServiceMock.Object,
            new Mock<INotificationSearchService>().Object,
            new Mock<INotificationSender>().Object,
            new Mock<IStoreService>().Object,
            () => roleManager,
            new Mock<IOrganizationMembershipService>().Object,
            Options.Create(new FrontendSecurityOptions()));
    }
}

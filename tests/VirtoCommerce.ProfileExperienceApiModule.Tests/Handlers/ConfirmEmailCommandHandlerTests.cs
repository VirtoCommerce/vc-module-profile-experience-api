using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class ConfirmEmailCommandHandlerTests : MoqHelper
    {
        private readonly Mock<IStoreService> _storeServiceMock = new();
        private readonly Mock<IMemberService> _memberServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ConfirmEmailCommandHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Handle_LockedContact_ApprovesContactAfterConfirmation()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com", MemberId = "contact-1", StoreId = "store-1" };
            var contact = new Contact { Id = "contact-1", Status = ModuleConstants.ContactStatuses.Locked };

            _userManagerMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

            _memberServiceMock.Setup(x => x.GetByIdAsync("contact-1", It.IsAny<string>())).ReturnsAsync(contact);
            _memberServiceMock.Setup(x => x.SaveChangesAsync(It.IsAny<Member[]>())).Returns(Task.CompletedTask);

            var handler = BuildHandler();
            var command = new ConfirmEmailCommand { UserId = "user-1", Token = "token" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _memberServiceMock.Verify(
                x => x.SaveChangesAsync(It.Is<Member[]>(members =>
                    members.Length == 1 && ((Contact)members[0]).Status == ModuleConstants.ContactStatuses.Approved)),
                Times.Once);

            // The same contact is reused for both the status update and the registration notification lookup
            _memberServiceMock.Verify(x => x.GetByIdAsync("contact-1", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyApprovedContact_DoesNotSaveContact()
        {
            // Arrange
            var user = new ApplicationUser { Id = "user-1", UserName = "test@test.com", MemberId = "contact-1", StoreId = "store-1" };
            var contact = new Contact { Id = "contact-1", Status = ModuleConstants.ContactStatuses.Approved };

            _userManagerMock.Setup(x => x.FindByIdAsync("user-1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.ConfirmEmailAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(x => x.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);

            _memberServiceMock.Setup(x => x.GetByIdAsync("contact-1", It.IsAny<string>())).ReturnsAsync(contact);

            var handler = BuildHandler();
            var command = new ConfirmEmailCommand { UserId = "user-1", Token = "token" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _memberServiceMock.Verify(x => x.SaveChangesAsync(It.IsAny<Member[]>()), Times.Never);
        }

        private ConfirmEmailCommandHandler BuildHandler()
        {
            var userManager = _userManagerMock.Object;

            return new ConfirmEmailCommandHandler(
                _storeServiceMock.Object,
                _memberServiceMock.Object,
                _mediatorMock.Object,
                () => userManager,
                Options.Create(new AuthorizationOptions()));
        }
    }
}

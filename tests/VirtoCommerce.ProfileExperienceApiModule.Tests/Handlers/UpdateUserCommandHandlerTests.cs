using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdateUserCommandHandlerTests
    {
        private const string UserId = "user-1";

        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly AuthorizationOptions _authorizationOptions = new();
        private ApplicationUser _savedUser;

        public UpdateUserCommandHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _userManagerMock
                .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(user => _savedUser = user)
                .ReturnsAsync(IdentityResult.Success);
        }

        [Fact]
        public async Task Handle_WithoutSecurityPermission_KeepsStoredSecurityData()
        {
            // Arrange: a customer tries to change its own security data (VCST-6970)
            var storedUser = SetupStoredUser();
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser
                {
                    Id = UserId,
                    UserName = "attacker",
                    Email = "new@example.com",
                    PhoneNumber = "+31 000 000",
                    PhotoUrl = "https://example.com/new.png",
                    UserType = "Manager",
                    IsAdministrator = true,
                    Roles = new List<Role> { new() { Name = "Store manager" } },
                    PasswordHash = "attacker-hash",
                    SecurityStamp = "attacker-stamp",
                    MemberId = "victim-contact",
                    StoreId = "other-store",
                    LockoutEnd = null,
                    LockoutEnabled = false,
                    AccessFailedCount = 0,
                    TwoFactorEnabled = false,
                },
            };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            _savedUser.Should().NotBeNull();

            _savedUser.Email.Should().Be("new@example.com");
            _savedUser.PhoneNumber.Should().Be("+31 000 000");
            _savedUser.PhotoUrl.Should().Be("https://example.com/new.png");

            _savedUser.UserName.Should().Be(storedUser.UserName);
            _savedUser.UserType.Should().Be("Customer");
            _savedUser.IsAdministrator.Should().BeFalse();
            _savedUser.Roles.Should().BeNull("roles must not be synchronized from the request");
            _savedUser.PasswordHash.Should().Be("stored-hash");
            _savedUser.SecurityStamp.Should().Be("stored-stamp");
            _savedUser.MemberId.Should().Be("contact-1");
            _savedUser.StoreId.Should().Be("store-1");
            _savedUser.LockoutEnd.Should().Be(storedUser.LockoutEnd);
            _savedUser.LockoutEnabled.Should().BeTrue();
            _savedUser.AccessFailedCount.Should().Be(3);
            _savedUser.TwoFactorEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithoutSecurityPermission_DoesNotChangeCachedStoredUser()
        {
            // Arrange
            var storedUser = SetupStoredUser();
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, UserName = "customer", Email = "new@example.com" },
            };

            // Act
            await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            _savedUser.Should().NotBeSameAs(storedUser);
            storedUser.Email.Should().Be("customer@example.com");
        }

        [Fact]
        public async Task Handle_EmailChanged_ResetsEmailConfirmation()
        {
            // Arrange
            SetupStoredUser();
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, UserName = "customer", Email = "new@example.com" },
            };

            // Act
            await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            _savedUser.EmailConfirmed.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_EmailNotChanged_KeepsEmailConfirmation()
        {
            // Arrange
            SetupStoredUser();
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, UserName = "customer", Email = "Customer@Example.com", PhoneNumber = "1" },
            };

            // Act
            await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            _savedUser.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithSecurityPermission_AppliesRequestedChanges()
        {
            // Arrange
            SetupStoredUser();
            var requestedUser = new ApplicationUser
            {
                Id = UserId,
                UserName = "customer",
                UserType = "Manager",
                Roles = new List<Role> { new() { Name = "Store manager" } },
            };
            var command = new UpdateUserCommand { ApplicationUser = requestedUser, CanUpdateSecurityData = true };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            _savedUser.Should().BeSameAs(requestedUser);
        }

        [Fact]
        public async Task Handle_UserNotFound_Fails()
        {
            // Arrange
            _userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync((ApplicationUser)null);
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId, UserName = "customer" } };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Handle_StoredUserIsNotEditable_Fails(bool canUpdateSecurityData)
        {
            // Arrange: the request sends an editable user name, but the id belongs to a non-editable user
            _authorizationOptions.NonEditableUsers = ["admin"];
            var storedUser = SetupStoredUser();
            storedUser.UserName = "admin";
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, UserName = "customer" },
                CanUpdateSecurityData = canUpdateSecurityData,
            };

            // Act
            var result = await CreateHandler().Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        private ApplicationUser SetupStoredUser()
        {
            var storedUser = new ApplicationUser
            {
                Id = UserId,
                UserName = "customer",
                Email = "customer@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+31 111 111",
                PhotoUrl = "https://example.com/old.png",
                UserType = "Customer",
                IsAdministrator = false,
                Roles = new List<Role> { new() { Name = "Organization maintainer" } },
                PasswordHash = "stored-hash",
                SecurityStamp = "stored-stamp",
                MemberId = "contact-1",
                StoreId = "store-1",
                LockoutEnabled = true,
                LockoutEnd = new System.DateTimeOffset(2030, 1, 1, 0, 0, 0, System.TimeSpan.Zero),
                AccessFailedCount = 3,
                TwoFactorEnabled = true,
            };

            _userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync(storedUser);

            return storedUser;
        }

        private UpdateUserCommandHandler CreateHandler()
        {
            return new UpdateUserCommandHandler(() => _userManagerMock.Object, Options.Create(_authorizationOptions));
        }
    }
}

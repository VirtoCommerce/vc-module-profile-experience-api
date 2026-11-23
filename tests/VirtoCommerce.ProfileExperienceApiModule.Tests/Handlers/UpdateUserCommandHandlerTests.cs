using System;
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

        [Fact]
        public async Task Handle_ProtectedFields_KeepsSavedValues()
        {
            // Arrange
            var user = CreateExistingUser();
            var userManager = CreateUserManager(user);
            var changes = new ApplicationUser
            {
                Id = UserId,
                UserName = "attacker",
                Email = "attacker@acme.com",
                SecurityStamp = "attacker-stamp",
                PasswordHash = "attacker-hash",
                LockoutEnabled = false,
                LockoutEnd = DateTimeOffset.MaxValue,
                AccessFailedCount = 0,
                TwoFactorEnabled = false,
                UserType = "Administrator",
                StoreId = "other-store",
                MemberId = "other-member",
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = false,
                PhotoUrl = "https://acme.com/new-photo.png",
                Roles = new List<Role> { new() { Id = "admin-role", Name = "Administrator" } },
            };
            var handler = CreateHandler(userManager);
            var command = new UpdateUserCommand { ApplicationUser = changes };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u =>
                u == user &&
                u.PhotoUrl == "https://acme.com/new-photo.png" &&
                u.UserName == "anna" &&
                u.Email == "anna@acme.com" &&
                u.EmailConfirmed &&
                u.SecurityStamp == "stamp" &&
                u.PasswordHash == "hash" &&
                u.LockoutEnabled &&
                u.LockoutEnd == null &&
                u.AccessFailedCount == 2 &&
                u.TwoFactorEnabled &&
                u.UserType == "Customer" &&
                u.StoreId == "store" &&
                u.MemberId == "member" &&
                u.PhoneNumberConfirmed &&
                u.Roles.Count == 0)), Times.Once);
            userManager.Verify(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            userManager.Verify(x => x.SetPhoneNumberAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NewPhoneNumber_SetsPhoneNumberWithUserManager()
        {
            // Arrange
            var user = CreateExistingUser();
            var userManager = CreateUserManager(user);
            var handler = CreateHandler(userManager);
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId, PhoneNumber = "+15550002", PhotoUrl = user.PhotoUrl } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.SetPhoneNumberAsync(user, "+15550002"), Times.Once);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NothingToChange_DoesNotUpdateUser()
        {
            // Arrange
            var user = CreateExistingUser();
            var userManager = CreateUserManager(user);
            var handler = CreateHandler(userManager);
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, PhoneNumber = user.PhoneNumber, PhotoUrl = user.PhotoUrl, Email = "attacker@acme.com" },
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.SetPhoneNumberAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NonEditableUser_RefusesRegardlessOfInputUserName()
        {
            // Arrange
            var user = CreateExistingUser();
            var userManager = CreateUserManager(user);
            var options = Options.Create(new AuthorizationOptions { NonEditableUsers = ["anna"] });
            var handler = new UpdateUserCommandHandler(() => userManager.Object, options);
            var command = new UpdateUserCommand
            {
                ApplicationUser = new ApplicationUser { Id = UserId, UserName = "someone-else", PhotoUrl = "https://acme.com/new-photo.png" },
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_UnknownUser_Fails()
        {
            // Arrange
            var userManager = CreateUserManager(CreateExistingUser());
            var handler = CreateHandler(userManager);
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = "unknown-user", PhotoUrl = "https://acme.com/new-photo.png" } };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        private static ApplicationUser CreateExistingUser()
        {
            return new ApplicationUser
            {
                Id = UserId,
                UserName = "anna",
                Email = "anna@acme.com",
                EmailConfirmed = true,
                SecurityStamp = "stamp",
                PasswordHash = "hash",
                LockoutEnabled = true,
                AccessFailedCount = 2,
                TwoFactorEnabled = true,
                UserType = "Customer",
                StoreId = "store",
                MemberId = "member",
                PhoneNumber = "+15550001",
                PhoneNumberConfirmed = true,
                PhotoUrl = "https://acme.com/photo.png",
                Roles = new List<Role>(),
            };
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManager(ApplicationUser existingUser)
        {
            var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            userManager
                .Setup(x => x.FindByIdAsync(existingUser.Id))
                .ReturnsAsync(existingUser);

            userManager
                .Setup(x => x.SetPhoneNumberAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager
                .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            return userManager;
        }

        private static UpdateUserCommandHandler CreateHandler(Mock<UserManager<ApplicationUser>> userManager)
        {
            return new UpdateUserCommandHandler(() => userManager.Object, Options.Create(new AuthorizationOptions()));
        }
    }
}

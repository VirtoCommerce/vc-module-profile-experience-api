using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class CreateUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_SecurityFieldsInRequest_CreatesCustomerWithProfileFieldsOnly()
        {
            // Arrange: an anonymous caller sends security fields with self-registration (VCST-6974)
            ApplicationUser createdUser = null;
            var accountServiceMock = new Mock<IAccountService>();
            accountServiceMock
                .Setup(x => x.CreateAccountAsync(It.IsAny<ApplicationUser>()))
                .Callback<ApplicationUser>(user => createdUser = user)
                .ReturnsAsync(IdentityResult.Success);

            var handler = new CreateUserCommandHandler(accountServiceMock.Object, new Mock<IMediator>().Object);

            var command = new CreateUserCommand
            {
                ApplicationUser = new ApplicationUser
                {
                    Id = "existing-contact-id",
                    UserName = "new-user",
                    Email = "new@example.com",
                    Password = "password",
                    PhoneNumber = "+31 000 000",
                    PhotoUrl = "https://example.com/photo.png",
                    StoreId = "store-1",
                    MemberId = "someone-elses-contact",
                    UserType = "Manager",
                    IsAdministrator = true,
                    EmailConfirmed = true,
                    TwoFactorEnabled = true,
                    LockoutEnabled = false,
                    Roles = new List<Role> { new() { Id = "role-1", Name = "Store manager" } },
                },
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            createdUser.Should().NotBeNull();
            createdUser.Id.Should().NotBe("existing-contact-id");
            createdUser.UserName.Should().Be("new-user");
            createdUser.Email.Should().Be("new@example.com");
            createdUser.Password.Should().Be("password");
            createdUser.PhoneNumber.Should().Be("+31 000 000");
            createdUser.PhotoUrl.Should().Be("https://example.com/photo.png");
            createdUser.StoreId.Should().Be("store-1");
            createdUser.UserType.Should().Be(nameof(UserType.Customer));
            createdUser.MemberId.Should().BeNull();
            createdUser.IsAdministrator.Should().BeFalse();
            createdUser.EmailConfirmed.Should().BeFalse();
            createdUser.TwoFactorEnabled.Should().BeFalse();
            createdUser.Roles.Should().BeNullOrEmpty();
        }
    }
}

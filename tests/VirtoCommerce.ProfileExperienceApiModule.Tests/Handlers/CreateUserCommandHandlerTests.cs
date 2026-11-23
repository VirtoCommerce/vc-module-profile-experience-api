using System;
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

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IAccountService> _accountServiceMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private ApplicationUser _createdUser;

    public CreateUserCommandHandlerTests()
    {
        _accountServiceMock
            .Setup(x => x.CreateAccountAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(user => _createdUser = user)
            .ReturnsAsync(IdentityResult.Success);
    }

    [Fact]
    public async Task Handle_WithoutSecurityPermission_CreatesCustomerWithProfileFieldsOnly()
    {
        // Arrange: an anonymous caller tries to create a user with security data (VCST-6974)
        var command = new CreateUserCommand
        {
            ApplicationUser = new ApplicationUser
            {
                Id = "victim-contact",
                UserName = "attacker",
                Email = "attacker@example.com",
                Password = "Passw0rd!",
                PhoneNumber = "+31 000 000",
                PhotoUrl = "https://example.com/photo.png",
                StoreId = "store-1",
                UserType = "Customer",
                IsAdministrator = true,
                Roles = new List<Role> { new() { Name = "Store manager" } },
                Logins = [new ApplicationUserLogin { LoginProvider = "Provider", ProviderKey = "key" }],
                MemberId = "victim-contact",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = true,
                PasswordHash = "attacker-hash",
                SecurityStamp = "attacker-stamp",
            },
        };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _createdUser.Should().NotBeNull();

        _createdUser.UserName.Should().Be("attacker");
        _createdUser.Email.Should().Be("attacker@example.com");
        _createdUser.Password.Should().Be("Passw0rd!");
        _createdUser.PhoneNumber.Should().Be("+31 000 000");
        _createdUser.PhotoUrl.Should().Be("https://example.com/photo.png");
        _createdUser.StoreId.Should().Be("store-1");

        _createdUser.Id.Should().NotBe("victim-contact");
        Guid.TryParse(_createdUser.Id, out _).Should().BeTrue();
        _createdUser.UserType.Should().Be("Customer");
        _createdUser.IsAdministrator.Should().BeFalse();
        _createdUser.Roles.Should().BeNullOrEmpty();
        _createdUser.Logins.Should().BeNullOrEmpty();
        _createdUser.MemberId.Should().BeNull();
        _createdUser.EmailConfirmed.Should().BeFalse();
        _createdUser.PhoneNumberConfirmed.Should().BeFalse();
        _createdUser.TwoFactorEnabled.Should().BeFalse();
        _createdUser.PasswordHash.Should().BeNull();
        _createdUser.SecurityStamp.Should().NotBe("attacker-stamp");
    }

    [Fact]
    public async Task Handle_WithoutSecurityPermission_SendsVerificationEmailForCreatedUser()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            ApplicationUser = new ApplicationUser { Id = "chosen-id", UserName = "customer", Email = "customer@example.com", UserType = "Customer" },
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        command.ApplicationUser.Should().BeSameAs(_createdUser);
        _mediatorMock.Verify(x => x.Send(It.IsAny<SendVerifyEmailCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSecurityPermission_CreatesRequestedUser()
    {
        // Arrange
        var requestedUser = new ApplicationUser
        {
            UserName = "customer",
            Email = "customer@example.com",
            UserType = "Customer",
            MemberId = "contact-1",
            Roles = new List<Role> { new() { Name = "Organization maintainer" } },
        };
        var command = new CreateUserCommand { ApplicationUser = requestedUser, CanSetSecurityData = true };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeTrue();
        _createdUser.Should().BeSameAs(requestedUser);
    }

    [Fact]
    public async Task Handle_CreateFailed_DoesNotSendVerificationEmail()
    {
        // Arrange
        _accountServiceMock
            .Setup(x => x.CreateAccountAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Duplicate user name." }));
        var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserName = "customer", UserType = "Customer" } };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.Succeeded.Should().BeFalse();
        _mediatorMock.Verify(x => x.Send(It.IsAny<SendVerifyEmailCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private CreateUserCommandHandler CreateHandler()
    {
        return new CreateUserCommandHandler(_accountServiceMock.Object, _mediatorMock.Object);
    }
}

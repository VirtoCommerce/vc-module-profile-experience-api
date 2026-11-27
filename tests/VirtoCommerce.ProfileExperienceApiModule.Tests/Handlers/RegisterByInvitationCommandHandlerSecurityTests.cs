using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class RegisterByInvitationCommandHandlerSecurityTests
{
    private const string _userId = "user-1";

    [Theory]
    [InlineData("Invited", false, false, null)]
    [InlineData("Invited", false, true, "InvitationNotPending")]
    [InlineData("Invited", true, false, "InvitationNotPending")]
    [InlineData("New", false, false, "InvitationNotPending")]
    [InlineData("Rejected", false, false, "InvitationNotPending")]
    [InlineData("Approved", false, false, "InvitationNotPending")]
    [InlineData("Locked", false, false, "ContactLocked")]
    [InlineData(null, false, false, "InvitationNotPending")]
    public async Task Handle_RegistersOnlyInvitedUserWhoHasNotAcceptedInvitation(string contactStatus, bool hasPassword, bool hasExternalLogin, string expectedErrorCode)
    {
        // Arrange
        var contact = CreateContact(contactStatus);
        var user = CreateUser(contact.Id, hasPassword);
        var userManagerMock = CreateUserManagerMock(user, hasExternalLogin);
        var memberServiceMock = CreateMemberServiceMock(contact);
        var command = CreateCommand();
        var handler = CreateHandler(userManagerMock, memberServiceMock);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedErrorCode, result.Errors.SingleOrDefault()?.Code);

        if (expectedErrorCode is null)
        {
            Assert.True(result.Succeeded);
        }
        else
        {
            Assert.False(result.Succeeded);
            userManagerMock.Verify(x => x.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            memberServiceMock.Verify(x => x.SaveChangesAsync(It.IsAny<Member[]>()), Times.Never);
        }
    }

    private static Contact CreateContact(string status)
    {
        return new Contact
        {
            Id = "contact-1",
            Status = status,
        };
    }

    private static ApplicationUser CreateUser(string memberId, bool hasPassword)
    {
        return new ApplicationUser
        {
            Id = _userId,
            MemberId = memberId,
            PasswordHash = hasPassword ? "password-hash" : null,
        };
    }

    private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock(ApplicationUser user, bool hasExternalLogin)
    {
        IList<UserLoginInfo> logins = hasExternalLogin
            ? [new UserLoginInfo("Google", "google-key", "Google")]
            : [];

        var userManagerMock = new Mock<UserManager<ApplicationUser>>(
            args: [Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null]);

        userManagerMock
            .Setup(x => x.FindByIdAsync(_userId))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetLoginsAsync(user))
            .ReturnsAsync(logins);

        userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.SetUserNameAsync(user, It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        return userManagerMock;
    }

    private static Mock<IMemberService> CreateMemberServiceMock(Contact contact)
    {
        var memberServiceMock = new Mock<IMemberService>();

        memberServiceMock
            .Setup(x => x.GetByIdAsync(contact.Id, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(contact);

        return memberServiceMock;
    }

    private static RegisterByInvitationCommand CreateCommand()
    {
        return new RegisterByInvitationCommand
        {
            UserId = _userId,
            Token = "password-reset-token",
            FirstName = "John",
            LastName = "Doe",
            Username = "john.doe",
            Password = "Password1!",
        };
    }

    private static RegisterByInvitationCommandHandler CreateHandler(Mock<UserManager<ApplicationUser>> userManagerMock, Mock<IMemberService> memberServiceMock)
    {
        var environmentMock = new Mock<IWebHostEnvironment>();

        environmentMock
            .Setup(x => x.EnvironmentName)
            .Returns(Environments.Development);

        return new RegisterByInvitationCommandHandler(
            environmentMock.Object,
            () => userManagerMock.Object,
            memberServiceMock.Object,
            Mock.Of<IStoreService>(),
            Mock.Of<IMediator>(),
            new RegisterByInvitationCommandValidator(Options.Create(new InputValidationOptions())),
            Mock.Of<IOrganizationMembershipService>(),
            Mock.Of<IOrganizationMembershipSearchService>());
    }
}

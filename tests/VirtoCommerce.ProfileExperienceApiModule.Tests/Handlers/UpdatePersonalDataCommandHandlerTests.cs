using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdatePersonalDataCommandHandlerTests
    {
        private const string UserId = "user-1";

        [Fact]
        public async Task Handle_InvalidPersonalData_NamesTheFailureByItsInputPath()
        {
            // updatePersonalData takes the data as command.personalData, so the failure is named PersonalData.FirstName.
            // Validation fails before the user is loaded, so the user manager is never needed.
            var handler = new UpdatePersonalDataCommandHandler(
                () => null,
                Options.Create(new AuthorizationOptions()),
                Mock.Of<IContactAggregateRepository>(),
                new PersonalDataValidator(Options.Create(new InputValidationOptions())));
            var command = new UpdatePersonalDataCommand
            {
                UserId = UserId,
                PersonalData = new PersonalData { FirstName = "Anna2", LastName = "Smith" },
            };

            var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));

            var failure = Assert.Single(exception.Errors);
            failure.PropertyName.Should().Be("PersonalData.FirstName");
            failure.ErrorCode.Should().Be(ModuleConstants.ValidationErrorCodes.InvalidCharacters);
        }

        [Fact]
        public async Task Handle_NewEmail_SetsEmailWithUserManager()
        {
            // Arrange
            var user = new ApplicationUser { Id = UserId, UserName = "anna", Email = "anna@acme.com", EmailConfirmed = true };
            var userManager = CreateUserManager(user);
            var handler = CreateHandler(userManager);

            // Act
            var result = await handler.Handle(CreateCommand("anna.smith@acme.com"), CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.SetEmailAsync(user, "anna.smith@acme.com"), Times.Once);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SameEmail_LeavesEmailUnchanged()
        {
            // Arrange
            var user = new ApplicationUser { Id = UserId, UserName = "anna", Email = "anna@acme.com", EmailConfirmed = true };
            var userManager = CreateUserManager(user);
            var handler = CreateHandler(userManager);

            // Act
            var result = await handler.Handle(CreateCommand("anna@acme.com"), CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
            userManager.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task Handle_EmailInOtherCase_KeepsConfirmation()
        {
            // Arrange
            var user = new ApplicationUser { Id = UserId, UserName = "anna", Email = "Anna@acme.com", EmailConfirmed = true };
            var userManager = CreateUserManager(user);
            var handler = CreateHandler(userManager);

            // Act
            var result = await handler.Handle(CreateCommand("anna@acme.com"), CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeTrue();
            userManager.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u == user && u.Email == "anna@acme.com" && u.EmailConfirmed)), Times.Once);
            userManager.Verify(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RefusedEmail_ReturnsError()
        {
            // Arrange
            var user = new ApplicationUser { Id = UserId, UserName = "anna", Email = "anna@acme.com", EmailConfirmed = true };
            var userManager = CreateUserManager(user);
            userManager
                .Setup(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail" }));
            var handler = CreateHandler(userManager);

            // Act
            var result = await handler.Handle(CreateCommand("taken@acme.com"), CancellationToken.None);

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Errors.Should().ContainSingle(x => x.Code == "DuplicateEmail");
        }

        private static Mock<UserManager<ApplicationUser>> CreateUserManager(ApplicationUser user)
        {
            var userManager = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            userManager
                .Setup(x => x.FindByIdAsync(user.Id))
                .ReturnsAsync(user);

            userManager
                .Setup(x => x.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            userManager
                .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            return userManager;
        }

        private static UpdatePersonalDataCommandHandler CreateHandler(Mock<UserManager<ApplicationUser>> userManager)
        {
            return new UpdatePersonalDataCommandHandler(
                () => userManager.Object,
                Options.Create(new AuthorizationOptions()),
                Mock.Of<IContactAggregateRepository>(),
                new PersonalDataValidator(Options.Create(new InputValidationOptions())));
        }

        private static UpdatePersonalDataCommand CreateCommand(string email)
        {
            return new UpdatePersonalDataCommand
            {
                UserId = UserId,
                PersonalData = new PersonalData { FirstName = "Anna", LastName = "Smith", Email = email },
            };
        }
    }
}

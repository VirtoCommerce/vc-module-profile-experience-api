using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
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
                UserId = "user-1",
                PersonalData = new PersonalData { FirstName = "Anna2", LastName = "Smith" },
            };

            var exception = await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));

            var failure = Assert.Single(exception.Errors);
            failure.PropertyName.Should().Be("PersonalData.FirstName");
            failure.ErrorCode.Should().Be(ModuleConstants.ValidationErrorCodes.InvalidCharacters);
        }
    }
}

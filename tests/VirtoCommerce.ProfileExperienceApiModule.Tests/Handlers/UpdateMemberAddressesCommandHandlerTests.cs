using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdateMemberAddressesCommandHandlerTests
    {
        private const string MemberId = "member-1";

        private readonly Mock<IMemberAggregateRootRepository> _repositoryMock = new();

        [Fact]
        public async Task Handle_InvalidAddresses_ReportsEveryFailureByAddressIndex()
        {
            // The storefront can send every saved address along with the edited one. The failures name the address by
            // its index in the command, and every invalid address is reported, not only the first one. Nothing is saved.
            var command = new UpdateMemberAddressesCommand(MemberId, [CreateAddress("Jane"), CreateAddress("Jane2"), CreateAddress("Ann_3")]);

            var exception = await Assert.ThrowsAsync<ValidationException>(() => CreateHandler().Handle(command, CancellationToken.None));

            exception.Errors.Select(x => x.PropertyName).Should().Equal("Addresses[1].FirstName", "Addresses[2].FirstName");
            exception.Errors.Should().OnlyContain(x => x.ErrorCode == ModuleConstants.ValidationErrorCodes.InvalidCharacters);
            // The message still names the property only, as when each address was validated on its own
            exception.Errors.First().ErrorMessage.Should().Be("'First Name' contains invalid characters.");
            _repositoryMock.Verify(x => x.SaveAsync(It.IsAny<IMemberAggregateRoot>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidAddresses_SavesThem()
        {
            // Validating the command instead of each address must not reject valid input
            var aggregate = new ContactAggregate { Member = new Contact { Addresses = [] } };
            _repositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(MemberId))
                .ReturnsAsync(aggregate);
            var command = new UpdateMemberAddressesCommand(MemberId, [CreateAddress("O'Connor")]);

            await CreateHandler().Handle(command, CancellationToken.None);

            aggregate.Member.Addresses.Select(x => x.FirstName).Should().Equal("O'Connor");
            _repositoryMock.Verify(x => x.SaveAsync(aggregate), Times.Once);
        }

        private UpdateMemberAddressesCommandHandler CreateHandler()
        {
            return new UpdateMemberAddressesCommandHandler(
                _repositoryMock.Object,
                new AddressValidator(Options.Create(new InputValidationOptions())));
        }

        private static Address CreateAddress(string firstName)
        {
            return new Address
            {
                FirstName = firstName,
                LastName = "Doe",
                Line1 = "456 Oak Avenue",
                City = "New York",
                PostalCode = "10001",
                CountryCode = "USA",
            };
        }
    }
}

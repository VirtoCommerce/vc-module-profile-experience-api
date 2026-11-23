using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdateContactCommandHandlerTests : MoqHelper
    {
        private readonly IProfileExperienceApiModuleMapper _mapper = new ProfileExperienceApiModuleMapper();

        [Fact]
        public async Task Handle_RequestWithDynamicProperties_UpdateDynamicPropertyCalled()
        {
            // Arrange
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            var dynamicPropertyUpdaterServiceMock = new Mock<IDynamicPropertyUpdaterService>();

            var contact = _fixture.Create<Contact>();
            contact.Emails = new List<string> { "initial@example.com" };
            var contactAggregate = new ContactAggregate { Member = contact };

            aggregateRepositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(contactAggregate);

            // Disable name validation to avoid test data issues with AutoFixture-generated names
            var disabledOptions = new InputValidationOptions { NameValidationPattern = null, EnableNoHtmlTagsValidation = false };
            var contactValidator = new NewContactValidator(Options.Create(disabledOptions));

            var handler = new UpdateContactCommandHandler(
                aggregateRepositoryMock.Object,
                dynamicPropertyUpdaterServiceMock.Object,
                Mock.Of<ICustomerPreferenceService>(),
                contactValidator,
                _mapper);

            var command = _fixture.Create<UpdateContactCommand>();
            command.Emails = null;

            // Act
            var aggregate = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Contains("initial@example.com", contact.Emails);

            dynamicPropertyUpdaterServiceMock.Verify(x => x.UpdateDynamicPropertyValues(It.Is<Contact>(x => x == contact),
                It.Is<IList<DynamicPropertyValue>>(x => x == command.DynamicProperties)), Times.Once);
        }

        [Fact]
        public async Task Handle_MembershipInRequest_IsAlwaysIgnored()
        {
            // Arrange (VCST-6974): the storefront API never changes a contact's organizations or customer groups
            var contact = new Contact { Id = "contact-1", FirstName = "John", LastName = "Doe", Name = "John Doe", Organizations = ["own-org"], Groups = ["Regular"] };
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>("contact-1"))
                .ReturnsAsync(new ContactAggregate { Member = contact });

            var disabledOptions = new InputValidationOptions { NameValidationPattern = null, EnableNoHtmlTagsValidation = false };
            var handler = new UpdateContactCommandHandler(
                aggregateRepositoryMock.Object,
                Mock.Of<IDynamicPropertyUpdaterService>(),
                Mock.Of<ICustomerPreferenceService>(),
                new NewContactValidator(Options.Create(disabledOptions)),
                _mapper);

            var command = new UpdateContactCommand
            {
                Id = "contact-1",
                FirstName = "Johnny",
                LastName = "Doe",
                Name = "Johnny Doe",
                Organizations = ["victim-org"],
                Groups = ["VIP"],
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal("Johnny", contact.FirstName);
            Assert.Equal(["own-org"], contact.Organizations);
            Assert.Equal(["Regular"], contact.Groups);
        }
    }
}

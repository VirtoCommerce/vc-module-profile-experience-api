using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
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
    public class CreateContactCommandHandlerTests : MoqHelper
    {
        [Fact]
        public async Task Handle_RequestWithDynamicProperties_UpdateDynamicPropertyCalled()
        {
            // Arragne
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            var aggregateFactoryMock = new Mock<IMemberAggregateFactory>();
            var dynamicPropertyUpdaterServiceMock = new Mock<IDynamicPropertyUpdaterService>();
            var mapperMock = new Mock<IProfileExperienceApiModuleMapper>();
            var disabledOptions = new InputValidationOptions { NameValidationPattern = null, EnableNoHtmlTagsValidation = false };
            var validator = new NewContactValidator(Options.Create(disabledOptions));

            var contact = _fixture.Create<Contact>();
            var contactAggregae = new ContactAggregate { Member = contact };
            aggregateFactoryMock
                .Setup(x => x.Create<ContactAggregate>(It.IsAny<Contact>()))
                .Returns(contactAggregae);

            var handler = new CreateContactCommandHandler(aggregateRepositoryMock.Object,
                aggregateFactoryMock.Object,
                dynamicPropertyUpdaterServiceMock.Object,
                mapperMock.Object,
                validator);

            var command = _fixture.Create<CreateContactCommand>();

            // Act
            var aggregate = await handler.Handle(command, CancellationToken.None);

            // Assert
            dynamicPropertyUpdaterServiceMock.Verify(x => x.UpdateDynamicPropertyValues(It.Is<Contact>(x => x == contact),
                It.Is<IList<DynamicPropertyValue>>(x => x == command.DynamicProperties)), Times.Once);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Handle_MembershipInRequest_AppliedOnlyWithPermission(bool canChangeMembership)
        {
            // Arrange (VCST-6974): an anonymous caller must not join organizations or customer groups
            var aggregateFactoryMock = new Mock<IMemberAggregateFactory>();
            Contact createdContact = null;
            aggregateFactoryMock
                .Setup(x => x.Create<ContactAggregate>(It.IsAny<Contact>()))
                .Callback<Member>(x => createdContact = (Contact)x)
                .Returns<Member>(x => new ContactAggregate { Member = x });

            var disabledOptions = new InputValidationOptions { NameValidationPattern = null, EnableNoHtmlTagsValidation = false };
            var handler = new CreateContactCommandHandler(Mock.Of<IContactAggregateRepository>(),
                aggregateFactoryMock.Object,
                Mock.Of<IDynamicPropertyUpdaterService>(),
                new ProfileExperienceApiModuleMapper(),
                new NewContactValidator(Options.Create(disabledOptions)));

            var command = new CreateContactCommand
            {
                FirstName = "John",
                LastName = "Doe",
                Name = "John Doe",
                Organizations = ["victim-org"],
                Groups = ["VIP"],
                CanChangeMembership = canChangeMembership,
            };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(createdContact);
            if (canChangeMembership)
            {
                Assert.Equal(["victim-org"], createdContact.Organizations);
                Assert.Equal(["VIP"], createdContact.Groups);
            }
            else
            {
                Assert.True(createdContact.Organizations is null || createdContact.Organizations.Count == 0);
                Assert.True(createdContact.Groups is null || createdContact.Groups.Count == 0);
            }
        }
    }
}

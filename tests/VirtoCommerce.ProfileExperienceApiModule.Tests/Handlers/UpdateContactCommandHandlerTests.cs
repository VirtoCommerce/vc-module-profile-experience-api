using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Mapping;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdateContactCommandHandlerTests : MoqHelper
    {
        private readonly IMapper _mapper;

        public UpdateContactCommandHandlerTests()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProfileMappingProfile>();
            });

            _mapper = configuration.CreateMapper();
        }

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
        public async Task Handle_OrganizationsAndGroupsInRequest_KeepsStoredMembership()
        {
            // Arrange: membership changes go through invite / accept / remove flows (VCST-6974)
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();

            var contact = _fixture.Create<Contact>();
            contact.Organizations = new List<string> { "org-1" };
            contact.Groups = new List<string> { "group-1" };

            aggregateRepositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(new ContactAggregate { Member = contact });

            var contactValidator = new NewContactValidator(Options.Create(new InputValidationOptions { NameValidationPattern = null, EnableNoHtmlTagsValidation = false }));

            var handler = new UpdateContactCommandHandler(aggregateRepositoryMock.Object,
                new Mock<IDynamicPropertyUpdaterService>().Object,
                Mock.Of<ICustomerPreferenceService>(),
                contactValidator,
                _mapper);

            var command = _fixture.Create<UpdateContactCommand>();
            command.Organizations = new List<string> { "foreign-org" };
            command.Groups = new List<string> { "vip" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(new[] { "org-1" }, contact.Organizations);
            Assert.Equal(new[] { "group-1" }, contact.Groups);
        }
    }
}

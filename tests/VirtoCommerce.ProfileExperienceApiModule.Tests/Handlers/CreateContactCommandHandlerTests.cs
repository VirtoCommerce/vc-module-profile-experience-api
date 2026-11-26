using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Tests.Helpers;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
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
            var mapperMock = new Mock<IMapper>();
            var validator = new NewContactValidator();

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

        [Fact]
        public async Task Handle_OrganizationsAndGroupsInRequest_AreNotMapped()
        {
            // Arrange: the storefront API never sets a contact's organizations or groups (VCST-6974)
            var aggregateFactoryMock = new Mock<IMemberAggregateFactory>();
            var mapperMock = new Mock<IMapper>();

            var contact = _fixture.Create<Contact>();
            aggregateFactoryMock
                .Setup(x => x.Create<ContactAggregate>(It.IsAny<Contact>()))
                .Returns(new ContactAggregate { Member = contact });

            CreateContactCommand mappedRequest = null;
            mapperMock
                .Setup(x => x.Map<Contact>(It.IsAny<object>()))
                .Callback<object>(source => mappedRequest = (CreateContactCommand)source)
                .Returns(contact);

            var handler = new CreateContactCommandHandler(new Mock<IContactAggregateRepository>().Object,
                aggregateFactoryMock.Object,
                new Mock<IDynamicPropertyUpdaterService>().Object,
                mapperMock.Object,
                new NewContactValidator());

            var command = _fixture.Create<CreateContactCommand>();
            command.Organizations = new List<string> { "foreign-org" };
            command.Groups = new List<string> { "vip" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(mappedRequest);
            Assert.Null(mappedRequest.Organizations);
            Assert.Null(mappedRequest.Groups);
        }
    }
}

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
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class UpdateContactCommandHandlerTests : MoqHelper
    {
        [Fact]
        public async Task Handle_RequestWithDynamicProperties_UpdateDynamicPropertyCalled()
        {
            // Arragne
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            var dynamicPropertyUpdaterServiceMock = new Mock<IDynamicPropertyUpdaterService>();
            var mapperMock = new Mock<IMapper>();

            var contact = _fixture.Create<Contact>();
            var contactAggregae = new ContactAggregate { Member = contact };

            aggregateRepositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(contactAggregae);

            var handler = new UpdateContactCommandHandler(aggregateRepositoryMock.Object,
                dynamicPropertyUpdaterServiceMock.Object,
                mapperMock.Object);

            var command = _fixture.Create<UpdateContactCommand>();

            // Act
            var aggregate = await handler.Handle(command, CancellationToken.None);

            // Assert
            dynamicPropertyUpdaterServiceMock.Verify(x => x.UpdateDynamicPropertyValues(It.Is<Contact>(x => x == contact),
                It.Is<IList<DynamicPropertyValue>>(x => x == command.DynamicProperties)), Times.Once);
        }
    }
}

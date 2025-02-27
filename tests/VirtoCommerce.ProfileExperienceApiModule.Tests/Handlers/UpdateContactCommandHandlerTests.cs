using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Mapping;
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
            // Arragne
            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            var dynamicPropertyUpdaterServiceMock = new Mock<IDynamicPropertyUpdaterService>();


            var contact = _fixture.Create<Contact>();
            contact.Emails = new List<string> { "initial@example.com" };
            var contactAggregae = new ContactAggregate { Member = contact };

            aggregateRepositoryMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(contactAggregae);

            var handler = new UpdateContactCommandHandler(aggregateRepositoryMock.Object,
                dynamicPropertyUpdaterServiceMock.Object,
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
    }
}

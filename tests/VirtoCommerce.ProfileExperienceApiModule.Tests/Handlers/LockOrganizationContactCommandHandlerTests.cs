using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class LockOrganizationContactCommandHandlerTests : MoqHelper
    {
        private readonly Mock<IContactAggregateRepository> _contactRepoMock = new();
        private readonly Mock<IOrganizationMembershipService> _membershipServiceMock = new();

        [Fact]
        public async Task Handle_EmptyOrganizationId_ThrowsArgumentException()
        {
            // Arrange
            var handler = BuildHandler();
            var command = new LockOrganizationContactCommand { UserId = "contact-1", OrganizationId = null };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ContactNotFound_ThrowsArgumentException()
        {
            // Arrange
            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync((ContactAggregate)null);

            var handler = BuildHandler();
            var command = new LockOrganizationContactCommand { UserId = "missing-contact", OrganizationId = "org-1" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoSecurityAccount_ReturnsContactWithoutLocking()
        {
            // Arrange
            var contact = new Contact { SecurityAccounts = [] };
            var aggregate = new ContactAggregate { Member = contact };

            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(aggregate);

            var handler = BuildHandler();
            var command = new LockOrganizationContactCommand { UserId = "contact-1", OrganizationId = "org-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Same(aggregate, result);
            _membershipServiceMock.Verify(x => x.LockAsync(It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MembershipFound_CallsLockAsync()
        {
            // Arrange
            const string securityUserId = "user-1";
            const string membershipId = "membership-1";

            var contact = new Contact
            {
                SecurityAccounts = [new ApplicationUser { Id = securityUserId }]
            };
            var aggregate = new ContactAggregate { Member = contact };

            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(aggregate);

            _membershipServiceMock
                .Setup(x => x.GetByUserAndOrgAsync(securityUserId, "org-1"))
                .ReturnsAsync(new OrganizationMembership { Id = membershipId });

            var handler = BuildHandler();
            var command = new LockOrganizationContactCommand { UserId = "contact-1", OrganizationId = "org-1" };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _membershipServiceMock.Verify(x => x.LockAsync(membershipId, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task Handle_MembershipNotFound_ReturnsContactWithoutLocking()
        {
            // Arrange
            const string securityUserId = "user-1";

            var contact = new Contact
            {
                SecurityAccounts = [new ApplicationUser { Id = securityUserId }]
            };
            var aggregate = new ContactAggregate { Member = contact };

            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(aggregate);

            _membershipServiceMock
                .Setup(x => x.GetByUserAndOrgAsync(securityUserId, "org-1"))
                .ReturnsAsync((OrganizationMembership)null);

            var handler = BuildHandler();
            var command = new LockOrganizationContactCommand { UserId = "contact-1", OrganizationId = "org-1" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Same(aggregate, result);
            _membershipServiceMock.Verify(x => x.LockAsync(It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
        }

        private LockOrganizationContactCommandHandler BuildHandler() =>
            new(_contactRepoMock.Object, _membershipServiceMock.Object);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class RemoveMemberFromOrganizationCommandHandlerTests
    {
        private const string ContactId = "contact1";
        private const string UserId = "user1";
        private const string OrgId = "org1";
        private const string MembershipId = "membership1";

        [Fact]
        public async Task Handle_MembershipExists_RemovesOrgFromContact_AndDeletesMembership()
        {
            // Arrange
            var contact = new Contact { Id = ContactId, Organizations = [OrgId], SecurityAccounts = [new ApplicationUser { Id = UserId }] };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            membershipSearchServiceMock
                .Setup(s => s.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == UserId && c.OrganizationId == OrgId),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = [new OrganizationMembership { Id = MembershipId }],
                });

            var membershipServiceMock = new Mock<IOrganizationMembershipService>();

            var handler = new RemoveMemberFromOrganizationCommandHandler(
                aggregateRepositoryMock.Object,
                membershipServiceMock.Object,
                membershipSearchServiceMock.Object);

            var command = new RemoveMemberFromOrganizationCommand { ContactId = ContactId, OrganizationId = OrgId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Same(contactAggregate, result);
            Assert.DoesNotContain(OrgId, contact.Organizations);
            aggregateRepositoryMock.Verify(r => r.SaveAsync(contactAggregate), Times.Once);
            membershipServiceMock.Verify(
                s => s.DeleteAsync(It.Is<IList<string>>(ids => ids.Count == 1 && ids[0] == MembershipId), false),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NoMembershipFound_DoesNotCallDelete()
        {
            // Arrange
            var contact = new Contact { Id = ContactId, Organizations = [OrgId], SecurityAccounts = [new ApplicationUser { Id = UserId }] };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            membershipSearchServiceMock
                .Setup(s => s.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [] });

            var membershipServiceMock = new Mock<IOrganizationMembershipService>();

            var handler = new RemoveMemberFromOrganizationCommandHandler(
                aggregateRepositoryMock.Object,
                membershipServiceMock.Object,
                membershipSearchServiceMock.Object);

            var command = new RemoveMemberFromOrganizationCommand { ContactId = ContactId, OrganizationId = OrgId };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            membershipServiceMock.Verify(s => s.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoSecurityAccount_SkipsMembershipLookup_AndDelete()
        {
            // Arrange — a contact without a linked user account (e.g. never registered): nothing to search for.
            var contact = new Contact { Id = ContactId, Organizations = [OrgId], SecurityAccounts = [] };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            var membershipServiceMock = new Mock<IOrganizationMembershipService>();

            var handler = new RemoveMemberFromOrganizationCommandHandler(
                aggregateRepositoryMock.Object,
                membershipServiceMock.Object,
                membershipSearchServiceMock.Object);

            var command = new RemoveMemberFromOrganizationCommand { ContactId = ContactId, OrganizationId = OrgId };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.DoesNotContain(OrgId, contact.Organizations);
            membershipSearchServiceMock.Verify(
                s => s.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Never);
            membershipServiceMock.Verify(s => s.DeleteAsync(It.IsAny<IList<string>>(), It.IsAny<bool>()), Times.Never);
        }
    }
}

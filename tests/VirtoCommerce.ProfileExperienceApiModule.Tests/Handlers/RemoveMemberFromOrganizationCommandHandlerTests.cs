using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core;
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
        public async Task Handle_MembershipExists_KeepsOrgOnContact_AndSetsMembershipDeleted()
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

            // Assert — the membership row is kept (soft-deleted via status), not hard-deleted, and the organization
            // stays in Contact.Organizations so the removed member remains visible/auditable in the admin members grid.
            Assert.Same(contactAggregate, result);
            Assert.Contains(OrgId, contact.Organizations);
            aggregateRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ContactAggregate>()), Times.Never);
            membershipServiceMock.Verify(
                s => s.SetStatusAsync(MembershipId, ModuleConstants.MembershipStatuses.Deleted),
                Times.Once);
            membershipServiceMock.Verify(s => s.DeleteAsync(It.IsAny<System.Collections.Generic.IList<string>>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Handle_NoMembershipFound_DoesNotCallSetStatus()
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
            membershipServiceMock.Verify(s => s.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_MultipleSecurityAccounts_ReusesResolvedMembership_SingleSearchCall()
        {
            // Arrange — the contact has two security accounts; the membership belongs to the second one.
            // The resolver's own search already returns the membership, so Handle must reuse it instead of
            // searching for it again by userId.
            const string secondUserId = "user2";
            var contact = new Contact
            {
                Id = ContactId,
                Organizations = [OrgId],
                SecurityAccounts = [new ApplicationUser { Id = UserId }, new ApplicationUser { Id = secondUserId }],
            };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            membershipSearchServiceMock
                .Setup(s => s.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c =>
                        c.UserIds != null && c.UserIds.Contains(UserId) && c.UserIds.Contains(secondUserId) && c.OrganizationId == OrgId),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = [new OrganizationMembership { Id = MembershipId, UserId = secondUserId }],
                });

            var membershipServiceMock = new Mock<IOrganizationMembershipService>();

            var handler = new RemoveMemberFromOrganizationCommandHandler(
                aggregateRepositoryMock.Object,
                membershipServiceMock.Object,
                membershipSearchServiceMock.Object);

            var command = new RemoveMemberFromOrganizationCommand { ContactId = ContactId, OrganizationId = OrgId };

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert — exactly one search call total (the multi-account resolution search)
            membershipSearchServiceMock.Verify(
                s => s.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Once);

            membershipServiceMock.Verify(
                s => s.SetStatusAsync(MembershipId, ModuleConstants.MembershipStatuses.Deleted),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NoSecurityAccount_SkipsMembershipLookup_AndSetStatus()
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
            Assert.Contains(OrgId, contact.Organizations);
            membershipSearchServiceMock.Verify(
                s => s.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Never);
            membershipServiceMock.Verify(s => s.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}

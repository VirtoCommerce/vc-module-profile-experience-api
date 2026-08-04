using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
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
    public class RejectOrganizationInviteCommandHandlerTests
    {
        private const string MemberId = "member1";
        private const string UserId = "user1";
        private const string OrgId = "org1";
        private const string MembershipId = "membership1";

        [Fact]
        public async Task Handle_PendingInvite_SetsRejected_KeepsOrgOnContact()
        {
            // Arrange
            var contact = new Contact
            {
                Id = MemberId,
                Organizations = [OrgId],
                SecurityAccounts = [new ApplicationUser { Id = UserId }],
            };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(MemberId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            membershipSearchServiceMock
                .Setup(s => s.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == UserId && c.OrganizationId == OrgId),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = [new OrganizationMembership { Id = MembershipId, Status = ModuleConstants.MembershipStatuses.Invited }],
                });

            var membershipServiceMock = new Mock<IOrganizationMembershipService>();

            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);
            userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = MemberId });

            var handler = new RejectOrganizationInviteCommandHandler(
                aggregateRepositoryMock.Object,
                membershipServiceMock.Object,
                membershipSearchServiceMock.Object,
                () => userManagerMock.Object);

            var command = new RejectOrganizationInviteCommand { UserId = UserId, OrganizationId = OrgId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert — the organization stays in Contact.Organizations: the admin members grid is driven by this
            // relation, so removing it here would hide the (still-auditable) rejected membership from that list.
            Assert.Same(contactAggregate, result);
            Assert.Contains(OrgId, contact.Organizations);
            membershipServiceMock.Verify(
                s => s.SetStatusAsync(MembershipId, ModuleConstants.MembershipStatuses.Rejected),
                Times.Once);
            aggregateRepositoryMock.Verify(r => r.SaveAsync(It.IsAny<ContactAggregate>()), Times.Never);
        }
    }
}

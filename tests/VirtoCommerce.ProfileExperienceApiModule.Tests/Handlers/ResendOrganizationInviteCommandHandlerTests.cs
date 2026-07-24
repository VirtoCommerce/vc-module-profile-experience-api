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
    public class ResendOrganizationInviteCommandHandlerTests
    {
        private const string MemberId = "member1";
        private const string UserId = "user1";
        private const string OrgId = "org1";
        private const string MembershipId = "membership1";

        [Fact]
        public async Task Handle_ResolvesMembershipId_AndDelegatesToSharedService()
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

            var inviteCustomerServiceMock = new Mock<IInviteCustomerService>();
            inviteCustomerServiceMock
                .Setup(s => s.ResendInviteAsync(MembershipId, "/confirm-invitation", "Reminder", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InviteCustomerResult { Succeeded = true, Errors = [] });

            var handler = new ResendOrganizationInviteCommandHandler(
                aggregateRepositoryMock.Object,
                membershipSearchServiceMock.Object,
                inviteCustomerServiceMock.Object);

            var command = new ResendOrganizationInviteCommand
            {
                MemberId = MemberId,
                OrganizationId = OrgId,
                UrlSuffix = "/confirm-invitation",
                Message = "Reminder",
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Empty(result.Errors);
            inviteCustomerServiceMock.Verify(
                s => s.ResendInviteAsync(MembershipId, "/confirm-invitation", "Reminder", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_NoSecurityAccount_ReturnsUserNotFoundError()
        {
            // Arrange
            var contact = new Contact { Id = MemberId, Organizations = [OrgId], SecurityAccounts = [] };
            var contactAggregate = new ContactAggregate { Member = contact };

            var aggregateRepositoryMock = new Mock<IContactAggregateRepository>();
            aggregateRepositoryMock
                .Setup(r => r.GetMemberAggregateRootByIdAsync<ContactAggregate>(MemberId))
                .ReturnsAsync(contactAggregate);

            var membershipSearchServiceMock = new Mock<IOrganizationMembershipSearchService>();
            var inviteCustomerServiceMock = new Mock<IInviteCustomerService>();

            var handler = new ResendOrganizationInviteCommandHandler(
                aggregateRepositoryMock.Object,
                membershipSearchServiceMock.Object,
                inviteCustomerServiceMock.Object);

            var command = new ResendOrganizationInviteCommand { MemberId = MemberId, OrganizationId = OrgId };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            var error = Assert.Single(result.Errors);
            Assert.Equal("UserNotFound", error.Code);
            inviteCustomerServiceMock.Verify(
                s => s.ResendInviteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}

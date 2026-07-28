using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class RegisterByInvitationCommandHandlerTests
    {
        private const string UserId = "user-1";
        private const string OrgId1 = "org-1";
        private const string OrgId2 = "org-2";

        private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();
        private readonly Mock<IOrganizationMembershipService> _membershipServiceMock = new();
        private readonly TestableHandler _handler;

        public RegisterByInvitationCommandHandlerTests()
        {
            _handler = new TestableHandler(
                new Mock<IWebHostEnvironment>().Object,
                () => new Mock<UserManager<ApplicationUser>>(
                    new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null).Object,
                new Mock<IMemberService>().Object,
                new Mock<IStoreService>().Object,
                new Mock<IMediator>().Object,
                new RegisterByInvitationCommandValidator(Options.Create(new InputValidationOptions())),
                _membershipServiceMock.Object,
                _membershipSearchServiceMock.Object);
        }

        [Fact]
        public async Task ApproveInvitedMemberships_MultipleOrganizations_SavesInSingleBatch()
        {
            // Arrange — the user was invited to two organizations; approving on registration must not
            // read-then-write each membership row individually.
            var membership1 = new OrganizationMembership { Id = "m1", UserId = UserId, OrganizationId = OrgId1, Status = ModuleConstants.MembershipStatuses.Invited };
            var membership2 = new OrganizationMembership { Id = "m2", UserId = UserId, OrganizationId = OrgId2, Status = ModuleConstants.MembershipStatuses.Invited };

            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == UserId && c.OrganizationIds.Count == 2),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [membership1, membership2], TotalCount = 2 });

            // Act
            await _handler.ApproveInvitedMembershipsPublicAsync(UserId, null, [OrgId1, OrgId2]);

            // Assert — one SaveChangesAsync call carrying both updated memberships, not two separate calls
            Assert.Equal(ModuleConstants.MembershipStatuses.Approved, membership1.Status);
            Assert.Equal(ModuleConstants.MembershipStatuses.Approved, membership2.Status);

            _membershipServiceMock.Verify(
                x => x.SaveChangesAsync(It.Is<IList<OrganizationMembership>>(list => list.Count == 2)),
                Times.Once);
            _membershipServiceMock.Verify(x => x.SetStatusAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ApproveInvitedMemberships_NoPendingInvites_DoesNotCallSaveChanges()
        {
            // Arrange
            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult { Results = [], TotalCount = 0 });

            // Act
            await _handler.ApproveInvitedMembershipsPublicAsync(UserId, OrgId1, null);

            // Assert
            _membershipServiceMock.Verify(x => x.SaveChangesAsync(It.IsAny<IList<OrganizationMembership>>()), Times.Never);
        }

        private class TestableHandler : RegisterByInvitationCommandHandler
        {
            public TestableHandler(
                IWebHostEnvironment environment,
                System.Func<UserManager<ApplicationUser>> userManager,
                IMemberService memberService,
                IStoreService storeService,
                IMediator mediator,
                RegisterByInvitationCommandValidator validator,
                IOrganizationMembershipService organizationMembershipService,
                IOrganizationMembershipSearchService organizationMembershipSearchService)
                : base(environment, userManager, memberService, storeService, mediator, validator, organizationMembershipService, organizationMembershipSearchService)
            {
            }

            public Task ApproveInvitedMembershipsPublicAsync(string userId, string organizationId, IList<string> contactOrganizationIds) =>
                ApproveInvitedMembershipsAsync(userId, organizationId, contactOrganizationIds);
        }
    }
}

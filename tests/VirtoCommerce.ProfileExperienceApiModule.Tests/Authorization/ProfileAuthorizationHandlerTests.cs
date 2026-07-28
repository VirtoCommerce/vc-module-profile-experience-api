using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Authorization
{
    public class ProfileAuthorizationHandlerTests
    {
        private const string UserId = "user-1";
        private const string ContactId = "contact-1";
        private const string OrgId = "org-1";

        private readonly Mock<IMemberService> _memberServiceMock = new();
        private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ProfileAuthorizationHandlerTests()
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [ClaimTypes.NameIdentifier];

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _memberServiceMock
                .Setup(x => x.GetByIdAsync(UserId, MemberResponseGroup.Default.ToString()))
                .ReturnsAsync((Member)null);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(UserId))
                .ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = ContactId });
        }

        [Fact]
        public async Task OrganizationAggregate_ApprovedNoOverride_Succeeds()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId], Status = ModuleConstants.MembershipStatuses.Approved });
            SetupMembership(null);

            // Act
            var authContext = await HandleAsync(BuildOrganizationResource());

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task OrganizationAggregate_GloballyRejected_NoMembershipOverride_Fails()
        {
            // Arrange — the contact was invited/added to the org but rejected/removed globally; no per-org
            // override exists, so the global Rejected status must block access to the org's data.
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId], Status = ModuleConstants.MembershipStatuses.Rejected });
            SetupMembership(null);

            // Act
            var authContext = await HandleAsync(BuildOrganizationResource());

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task OrganizationAggregate_MembershipOverrideApproved_SucceedsDespiteGlobalRejected()
        {
            // Arrange — a per-org Approved override wins over the globally Rejected contact status.
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId], Status = ModuleConstants.MembershipStatuses.Rejected });
            SetupMembership(new OrganizationMembership { OrganizationId = OrgId, Status = ModuleConstants.MembershipStatuses.Approved });

            // Act
            var authContext = await HandleAsync(BuildOrganizationResource());

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task OrganizationAggregate_LockedMembership_Fails()
        {
            // Arrange — locked membership blocks access regardless of the (otherwise fine) effective status.
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId], Status = ModuleConstants.MembershipStatuses.Approved });
            SetupMembership(new OrganizationMembership { OrganizationId = OrgId, Status = ModuleConstants.MembershipStatuses.Approved, IsLocked = true });

            // Act
            var authContext = await HandleAsync(BuildOrganizationResource());

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task OrganizationAggregate_NotMemberOfOrganization_Fails_WithoutQueryingMemberships()
        {
            // Arrange — the contact was never added to this org's Organizations list at all.
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [], Status = ModuleConstants.MembershipStatuses.Approved });

            // Act
            var authContext = await HandleAsync(BuildOrganizationResource());

            // Assert
            Assert.False(authContext.HasSucceeded);
            _membershipSearchServiceMock.Verify(
                x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()),
                Times.Never);
        }

        private void SetupCurrentContact(Contact contact)
        {
            _memberServiceMock
                .Setup(x => x.GetByIdAsync(ContactId, MemberResponseGroup.Default.ToString()))
                .ReturnsAsync(contact);
        }

        private void SetupMembership(OrganizationMembership membership)
        {
            _membershipSearchServiceMock
                .Setup(x => x.SearchAsync(
                    It.Is<OrganizationMembershipSearchCriteria>(c => c.UserId == UserId && c.OrganizationId == OrgId),
                    It.IsAny<bool>()))
                .ReturnsAsync(new OrganizationMembershipSearchResult
                {
                    Results = membership == null ? [] : [membership],
                });
        }

        private static OrganizationAggregate BuildOrganizationResource() =>
            new() { Member = new Organization { Id = OrgId } };

        private async Task<AuthorizationHandlerContext> HandleAsync(object resource)
        {
            var handler = new ProfileAuthorizationHandler(
                _memberServiceMock.Object,
                _membershipSearchServiceMock.Object,
                () => _userManagerMock.Object);

            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId)]));
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], principal, resource);

            await handler.HandleAsync(authContext);

            return authContext;
        }
    }
}

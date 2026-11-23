using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Authorization
{
    public class ProfileAuthorizationHandlerTests
    {
        private const string UserId = "user-1";
        private const string ContactId = "contact-1";
        private const string OrgId = "org-1";
        private const string OtherUserId = "user-2";
        private const string OtherContactId = "contact-2";

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

        [Fact]
        public async Task UpdateUser_Self_Succeeds()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId } };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_SelfWithoutContact_Succeeds()
        {
            // Arrange
            SetupCurrentContact(null);
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId } };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_OtherUserOfSameOrganization_WithoutPermission_Fails()
        {
            // Arrange: before VCST-6970 any organization member could rewrite colleagues (password hash, roles, user type)
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupOtherUserInOrganization();
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_OtherUserOfSameOrganization_EvenWithSecurityUpdatePermission_Fails()
        {
            // Arrange (VCST-6974): the storefront API updates the caller's own account only; managing other
            // users is a back-office operation even for a caller that holds platform:security:update.
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupOtherUserInOrganization();
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };

            // Act
            var authContext = await HandleAsync(command, OrgClaim(OrgId), new Claim(PlatformConstants.Security.Claims.PermissionClaimType, PlatformConstants.Security.Permissions.SecurityUpdate));

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_UserIdEqualsOwnContactId_IsNotTreatedAsSelf()
        {
            // Arrange: the old check compared the target user id with the caller's contact id
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = ContactId } };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_Anonymous_Fails()
        {
            // Arrange
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = null } };
            var handler = CreateHandler();
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), command);

            // Act
            await handler.HandleAsync(authContext);

            // Assert
            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_Administrator_Succeeds()
        {
            // Arrange
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };

            // Act
            var authContext = await HandleAsync(command, new Claim(ClaimTypes.Role, PlatformConstants.Security.SystemRoles.Administrator));

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task CreateUser_Anonymous_Succeeds()
        {
            // Arrange (VCST-6974): anonymous self-registration is allowed; security data is stripped by the handler
            var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserType = "Customer" } };
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), command);

            // Act
            await CreateHandler().HandleAsync(authContext);

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task CreateContact_Anonymous_Succeeds()
        {
            // Arrange
            var command = new CreateContactCommand();
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), command);

            // Act
            await CreateHandler().HandleAsync(authContext);

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_Self_Succeeds()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateContactCommand { Id = ContactId };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.True(authContext.HasSucceeded);
        }

        // VCST-6974 #5: editing another member's contact requires the org-edit permission.
        [Fact]
        public async Task UpdateContact_OtherMemberOfCurrentOrg_WithoutOrgEditPermission_Fails()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupContact(OtherContactId, new Contact { Id = OtherContactId, Organizations = [OrgId] });
            var command = new UpdateContactCommand { Id = OtherContactId };

            var authContext = await HandleAsync(command, OrgClaim(OrgId));

            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_OtherMemberOfCurrentOrg_WithOrgEditPermission_Succeeds()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupContact(OtherContactId, new Contact { Id = OtherContactId, Organizations = [OrgId] });
            var command = new UpdateContactCommand { Id = OtherContactId };

            var authContext = await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim());

            Assert.True(authContext.HasSucceeded);
        }

        // VCST-6974 #9: sharing some OTHER organization is not enough; only the current organization counts.
        [Fact]
        public async Task UpdateContact_OtherMemberSharesDifferentOrg_WithOrgEditPermission_Fails()
        {
            const string otherOrg = "org-2";
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId, otherOrg] });
            SetupContact(OtherContactId, new Contact { Id = OtherContactId, Organizations = [otherOrg] });
            var command = new UpdateContactCommand { Id = OtherContactId };

            // current organization is OrgId, but the target is only in otherOrg
            var authContext = await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim());

            Assert.False(authContext.HasSucceeded);
        }

        // VCST-6974 #10: removing a member uses the caller's current organization, not an org from the input.
        [Fact]
        public async Task RemoveMember_OrganizationNotCurrent_Fails()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId, "foreign-org"] });
            SetupContact(OtherContactId, new Contact { Id = OtherContactId, Organizations = [OrgId, "foreign-org"] });
            var command = new RemoveMemberFromOrganizationCommand { ContactId = OtherContactId, OrganizationId = "foreign-org" };

            var authContext = await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim());

            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task RemoveMember_CurrentOrganization_WithOrgEditPermission_Succeeds()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupContact(OtherContactId, new Contact { Id = OtherContactId, Organizations = [OrgId] });
            var command = new RemoveMemberFromOrganizationCommand { ContactId = OtherContactId, OrganizationId = OrgId };

            var authContext = await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim());

            Assert.True(authContext.HasSucceeded);
        }

        // VCST-6974 #6: an invitation without an organization id no longer bypasses the resource check.
        [Fact]
        public async Task InviteUser_WithoutOrganizationId_Fails()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            _userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = ContactId, StoreId = "store-1" });
            var command = new InviteUserCommand { StoreId = "store-1", OrganizationId = null };

            var authContext = await HandleAsync(command, OrgClaim(OrgId), InviteClaim());

            Assert.False(authContext.HasSucceeded);
        }

        [Fact]
        public async Task InviteUser_CurrentOrganization_WithInvitePermission_Succeeds()
        {
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            _userManagerMock.Setup(x => x.FindByIdAsync(UserId)).ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = ContactId, StoreId = "store-1" });
            var command = new InviteUserCommand { StoreId = "store-1", OrganizationId = OrgId };

            var authContext = await HandleAsync(command, OrgClaim(OrgId), InviteClaim());

            Assert.True(authContext.HasSucceeded);
        }

        private static Claim OrgClaim(string organizationId) =>
            new(CustomerModule.Core.ModuleConstants.Security.Claims.OrganizationId, organizationId);

        private static Claim OrgEditClaim() =>
            new(PlatformConstants.Security.Claims.PermissionClaimType, ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions.MyOrganizationEdit);

        private static Claim InviteClaim() =>
            new(PlatformConstants.Security.Claims.PermissionClaimType, ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions.MyOrganizationUserInvite);

        private void SetupContact(string id, Contact contact)
        {
            _memberServiceMock
                .Setup(x => x.GetByIdAsync(id, MemberResponseGroup.Default.ToString()))
                .ReturnsAsync(contact);
        }

        private void SetupOtherUserInOrganization()
        {
            _memberServiceMock
                .Setup(x => x.GetByIdAsync(OtherUserId, MemberResponseGroup.Default.ToString()))
                .ReturnsAsync((Member)null);
            _userManagerMock
                .Setup(x => x.FindByIdAsync(OtherUserId))
                .ReturnsAsync(new ApplicationUser { Id = OtherUserId, MemberId = OtherContactId });
            _memberServiceMock
                .Setup(x => x.GetByIdAsync(OtherContactId, MemberResponseGroup.Default.ToString()))
                .ReturnsAsync(new Contact { Id = OtherContactId, Organizations = [OrgId] });
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

        private ProfileAuthorizationHandler CreateHandler() =>
            new(_memberServiceMock.Object,
                _membershipSearchServiceMock.Object,
                () => _userManagerMock.Object);

        private async Task<AuthorizationHandlerContext> HandleAsync(object resource, params Claim[] extraClaims)
        {
            var handler = CreateHandler();

            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId), .. extraClaims], "test"));
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], principal, resource);

            await handler.HandleAsync(authContext);

            return authContext;
        }
    }
}

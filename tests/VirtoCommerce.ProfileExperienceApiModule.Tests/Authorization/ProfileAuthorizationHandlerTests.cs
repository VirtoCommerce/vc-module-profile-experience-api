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
        public async Task UpdateUser_Self_Succeeds_WithoutSecurityData()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId } };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.False(command.CanUpdateSecurityData);
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
        public async Task UpdateUser_FlagFromRequest_IsOverwritten()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId }, CanUpdateSecurityData = true };

            // Act
            await HandleAsync(command);

            // Assert
            Assert.False(command.CanUpdateSecurityData);
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
        public async Task UpdateUser_OtherUserOfSameOrganization_WithSecurityUpdatePermission_Succeeds()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            SetupOtherUserInOrganization();
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };

            // Act
            var authContext = await HandleAsync(command, new Claim(PlatformConstants.Security.Claims.PermissionClaimType, PlatformConstants.Security.Permissions.SecurityUpdate));

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.True(command.CanUpdateSecurityData);
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
            Assert.False(command.CanUpdateSecurityData);
        }

        [Fact]
        public async Task UpdateUser_Administrator_Succeeds_WithSecurityData()
        {
            // Arrange
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };

            // Act
            var authContext = await HandleAsync(command, new Claim(ClaimTypes.Role, PlatformConstants.Security.SystemRoles.Administrator));

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.True(command.CanUpdateSecurityData);
        }

        [Fact]
        public async Task CreateUser_Anonymous_Succeeds_WithoutSecurityData()
        {
            // Arrange (VCST-6974)
            var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserType = "Customer" }, CanSetSecurityData = true };
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), command);

            // Act
            await CreateHandler().HandleAsync(authContext);

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.False(command.CanSetSecurityData);
        }

        [Fact]
        public async Task CreateUser_WithSecurityCreatePermission_SetsSecurityDataFlag()
        {
            // Arrange
            var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserType = "Customer" } };

            // Act
            var authContext = await HandleAsync(command, new Claim(PlatformConstants.Security.Claims.PermissionClaimType, PlatformConstants.Security.Permissions.SecurityCreate));

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.True(command.CanSetSecurityData);
        }

        [Fact]
        public async Task CreateContact_Anonymous_Succeeds_WithoutMembership()
        {
            // Arrange
            var command = new CreateContactCommand { CanChangeMembership = true };
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], new ClaimsPrincipal(new ClaimsIdentity()), command);

            // Act
            await CreateHandler().HandleAsync(authContext);

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.False(command.CanChangeMembership);
        }

        [Fact]
        public async Task CreateContact_WithCustomerCreatePermission_SetsMembershipFlag()
        {
            // Arrange
            var command = new CreateContactCommand();

            // Act
            await HandleAsync(command, new Claim(PlatformConstants.Security.Claims.PermissionClaimType, CustomerModule.Core.ModuleConstants.Security.Permissions.Create));

            // Assert
            Assert.True(command.CanChangeMembership);
        }

        [Fact]
        public async Task UpdateContact_Self_Succeeds_WithoutMembership()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateContactCommand { Id = ContactId, CanChangeMembership = true };

            // Act
            var authContext = await HandleAsync(command);

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.False(command.CanChangeMembership);
        }

        [Fact]
        public async Task UpdateContact_WithCustomerUpdatePermission_SetsMembershipFlag()
        {
            // Arrange
            SetupCurrentContact(new Contact { Id = ContactId, Organizations = [OrgId] });
            var command = new UpdateContactCommand { Id = ContactId };

            // Act
            var authContext = await HandleAsync(command, new Claim(PlatformConstants.Security.Claims.PermissionClaimType, CustomerModule.Core.ModuleConstants.Security.Permissions.Update));

            // Assert
            Assert.True(authContext.HasSucceeded);
            Assert.True(command.CanChangeMembership);
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

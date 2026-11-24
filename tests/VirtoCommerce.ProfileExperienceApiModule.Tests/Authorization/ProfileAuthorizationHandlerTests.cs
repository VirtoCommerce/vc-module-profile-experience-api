using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;
using ProfilePermissions = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Authorization
{
    // VCST-6974 backport: strict own-account / own-current-organization authorization.
    public class ProfileAuthorizationHandlerTests
    {
        private const string UserId = "user-1";
        private const string ContactId = "contact-1";
        private const string OrgId = "org-1";
        private const string OtherUserId = "user-2";
        private const string OtherContactId = "contact-2";
        private const string OrgClaimType = "organization_id";

        // The caller's own member and the members referenced by a resource, keyed by id.
        private readonly Dictionary<string, Member> _members = [];
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public ProfileAuthorizationHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(UserId))
                .ReturnsAsync(new ApplicationUser { Id = UserId, MemberId = ContactId, StoreId = "store-1" });
        }

        // #2 - anonymous caller may create a customer, but never an administrator.
        [Fact]
        public async Task CreateUser_Customer_Succeeds()
        {
            var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserType = "Customer", IsAdministrator = false } };
            Assert.True((await HandleAsync(command)).HasSucceeded);
        }

        [Fact]
        public async Task CreateUser_Administrator_Fails()
        {
            var command = new CreateUserCommand { ApplicationUser = new ApplicationUser { UserType = "Customer", IsAdministrator = true } };
            Assert.False((await HandleAsync(command)).HasSucceeded);
        }

        // #7 - updateUser authorizes the caller's own account only.
        [Fact]
        public async Task UpdateUser_OwnAccount_Succeeds()
        {
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = UserId } };
            Assert.True((await HandleAsync(command)).HasSucceeded);
        }

        [Fact]
        public async Task UpdateUser_AnotherUser_Fails()
        {
            var command = new UpdateUserCommand { ApplicationUser = new ApplicationUser { Id = OtherUserId } };
            Assert.False((await HandleAsync(command)).HasSucceeded);
        }

        // #5/#8 - updateContact: own contact OK; a colleague needs the org-edit permission AND the current org.
        [Fact]
        public async Task UpdateContact_OwnContact_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            var command = new UpdateContactCommand { Id = ContactId };
            Assert.True((await HandleAsync(command, OrgClaim(OrgId))).HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_ColleagueWithoutPermission_Fails()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.False((await HandleAsync(command, OrgClaim(OrgId))).HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_ColleagueWithPermissionInCurrentOrg_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.True((await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_ColleagueInDifferentOrg_Fails()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, ["other-org"]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.False((await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim())).HasSucceeded);
        }

        // #10 - removeMember: the caller must be a member of the organization being acted on.
        [Fact]
        public async Task RemoveMember_ForeignOrganization_Fails()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId, "foreign-org"]);
            var command = new RemoveMemberFromOrganizationCommand { ContactId = OtherContactId, OrganizationId = "foreign-org" };
            Assert.False((await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task RemoveMember_SharedOrganization_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId]);
            var command = new RemoveMemberFromOrganizationCommand { ContactId = OtherContactId, OrganizationId = OrgId };
            Assert.True((await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim())).HasSucceeded);
        }

        // #6 - inviteUser into an organization the caller is not a member of is denied.
        [Fact]
        public async Task InviteUser_ForeignOrganization_Fails()
        {
            SetupCurrentContact([OrgId]);
            var command = new InviteUserCommand { StoreId = "store-1", OrganizationId = "foreign-org" };
            Assert.False((await HandleAsync(command, OrgClaim(OrgId))).HasSucceeded);
        }

        [Fact]
        public async Task InviteUser_OwnOrganization_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            var command = new InviteUserCommand { StoreId = "store-1", OrganizationId = OrgId };
            Assert.True((await HandleAsync(command, OrgClaim(OrgId))).HasSucceeded);
        }

        // Customer module before 3.811 issues no organization_id claim: "current organization" = any organization of the caller.
        [Fact]
        public async Task UpdateContact_NoOrgClaim_ColleagueWithPermissionInSharedOrg_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.True((await HandleAsync(command, OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_NoOrgClaim_ColleagueWithoutPermission_Fails()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, [OrgId]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.False((await HandleAsync(command)).HasSucceeded);
        }

        [Fact]
        public async Task UpdateContact_NoOrgClaim_ContactInDifferentOrg_Fails()
        {
            SetupCurrentContact([OrgId]);
            SetupContact(OtherContactId, ["other-org"]);
            var command = new UpdateContactCommand { Id = OtherContactId };
            Assert.False((await HandleAsync(command, OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task UpdateOrganization_NoOrgClaim_OwnOrgWithPermission_Succeeds()
        {
            SetupCurrentContact([OrgId]);
            var command = new UpdateOrganizationCommand { Id = OrgId };
            Assert.True((await HandleAsync(command, OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task UpdateOrganization_NoOrgClaim_ForeignOrg_Fails()
        {
            SetupCurrentContact([OrgId]);
            var command = new UpdateOrganizationCommand { Id = "foreign-org" };
            Assert.False((await HandleAsync(command, OrgEditClaim())).HasSucceeded);
        }

        [Fact]
        public async Task UpdateOrganization_OrgClaim_NonCurrentOrg_Fails()
        {
            SetupCurrentContact([OrgId, "second-org"]);
            var command = new UpdateOrganizationCommand { Id = "second-org" };
            Assert.False((await HandleAsync(command, OrgClaim(OrgId), OrgEditClaim())).HasSucceeded);
        }

        private static Claim OrgClaim(string organizationId) => new(OrgClaimType, organizationId);

        private static Claim OrgEditClaim() =>
            new(PlatformConstants.Security.Claims.PermissionClaimType, ProfilePermissions.MyOrganizationEdit);

        private void SetupCurrentContact(string[] organizations) =>
            _members[ContactId] = new Contact { Id = ContactId, Organizations = [.. organizations] };

        private void SetupContact(string id, string[] organizations) =>
            _members[id] = new Contact { Id = id, Organizations = [.. organizations] };

        // Resolves the caller's own member (looked up by user id -> MemberId) and any contact by id,
        // without depending on the member-service response-group argument shape.
        private sealed class TestableHandler(IDictionary<string, Member> members, System.Func<UserManager<ApplicationUser>> umf)
            : ProfileAuthorizationHandler(new Mock<IMemberService>().Object, umf)
        {
            protected override Task<Member> GetCustomerAsync(string customerId, UserManager<ApplicationUser> userManager)
            {
                if (customerId == UserId)
                {
                    members.TryGetValue(ContactId, out var self);
                    return Task.FromResult(self);
                }
                members.TryGetValue(customerId, out var member);
                return Task.FromResult(member);
            }
        }

        private async Task<AuthorizationHandlerContext> HandleAsync(object resource, params Claim[] extraClaims)
        {
            var handler = new TestableHandler(_members, () => _userManagerMock.Object);
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", UserId), .. extraClaims], "test"));
            var authContext = new AuthorizationHandlerContext([new ProfileAuthorizationRequirement()], principal, resource);
            await handler.HandleAsync(authContext);
            return authContext;
        }
    }
}

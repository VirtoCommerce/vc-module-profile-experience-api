using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
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
    public class ChangeOrganizationContactRoleCommandHandlerTests : MoqHelper
    {
        private readonly Mock<IContactAggregateRepository> _contactRepoMock = new();
        private readonly Mock<IOrganizationMembershipService> _membershipServiceMock = new();
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<Role>> _roleManagerMock;

        public ChangeOrganizationContactRoleCommandHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                new Mock<IUserStore<ApplicationUser>>().Object, null, null, null, null, null, null, null, null);

            _roleManagerMock = new Mock<RoleManager<Role>>(
                new Mock<IRoleStore<Role>>().Object, null, null, null, null);
        }

        [Fact]
        public async Task Handle_EmptyOrganizationId_ReturnsError()
        {
            // Arrange
            var handler = BuildHandler();
            var command = new ChangeOrganizationContactRoleCommand
            {
                UserId = "contact-1",
                OrganizationId = null,
                RoleIds = ["role-1"]
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "OrganizationIdRequired");
        }

        [Fact]
        public async Task Handle_ContactNotFound_ThrowsArgumentException()
        {
            // Arrange
            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync((ContactAggregate)null);

            var handler = BuildHandler();
            var command = new ChangeOrganizationContactRoleCommand
            {
                UserId = "missing-contact",
                OrganizationId = "org-1",
                RoleIds = ["role-1"]
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_MembershipNotFound_ReturnsError()
        {
            // Arrange
            const string securityUserId = "user-1";

            SetupContactAggregate(securityUserId);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(securityUserId))
                .ReturnsAsync(new ApplicationUser { Id = securityUserId, UserName = "test@test.com" });

            _roleManagerMock
                .Setup(x => x.FindByIdAsync("role-1"))
                .ReturnsAsync(new Role { Id = "role-1", Name = "TestRole", NormalizedName = "TESTROLE" });

            _membershipServiceMock
                .Setup(x => x.GetByUserAndOrgAsync(securityUserId, "org-1"))
                .ReturnsAsync((OrganizationMembership)null);

            var handler = BuildHandler();
            var command = new ChangeOrganizationContactRoleCommand
            {
                UserId = "contact-1",
                OrganizationId = "org-1",
                RoleIds = ["role-1"]
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Code == "MembershipNotFound");
        }

        [Fact]
        public async Task Handle_ValidRequest_UpdatesMembershipRoles()
        {
            // Arrange
            const string securityUserId = "user-1";
            const string membershipId = "membership-1";

            SetupContactAggregate(securityUserId);

            _userManagerMock
                .Setup(x => x.FindByIdAsync(securityUserId))
                .ReturnsAsync(new ApplicationUser { Id = securityUserId, UserName = "test@test.com" });

            _roleManagerMock
                .Setup(x => x.FindByIdAsync("role-1"))
                .ReturnsAsync(new Role { Id = "role-1", Name = "TestRole", NormalizedName = "TESTROLE" });

            var membership = new OrganizationMembership { Id = membershipId, Roles = [] };
            _membershipServiceMock
                .Setup(x => x.GetByUserAndOrgAsync(securityUserId, "org-1"))
                .ReturnsAsync(membership);

            var handler = BuildHandler();
            var command = new ChangeOrganizationContactRoleCommand
            {
                UserId = "contact-1",
                OrganizationId = "org-1",
                RoleIds = ["role-1"]
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            _membershipServiceMock.Verify(
                x => x.SaveChangesAsync(It.Is<IList<OrganizationMembership>>(list =>
                    list.Count == 1 && list[0].Id == membershipId && list[0].Roles.Count == 1)),
                Times.Once);
        }

        private void SetupContactAggregate(string securityUserId)
        {
            var contact = new Contact
            {
                SecurityAccounts = [new() { Id = securityUserId }]
            };
            _contactRepoMock
                .Setup(x => x.GetMemberAggregateRootByIdAsync<ContactAggregate>(It.IsAny<string>()))
                .ReturnsAsync(new ContactAggregate { Member = contact });
        }

        private ChangeOrganizationContactRoleCommandHandler BuildHandler()
        {
            var userManager = _userManagerMock.Object;
            var roleManager = _roleManagerMock.Object;

            return new ChangeOrganizationContactRoleCommandHandler(
                () => userManager,
                () => roleManager,
                _membershipServiceMock.Object,
                _contactRepoMock.Object,
                Options.Create(new AuthorizationOptions()));
        }
    }
}

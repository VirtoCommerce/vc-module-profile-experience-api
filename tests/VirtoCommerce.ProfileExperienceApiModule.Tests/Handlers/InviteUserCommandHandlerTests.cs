using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class InviteUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_MapsCommandToInviteCustomerRequest_AndDelegatesToSharedService()
        {
            // Arrange
            InviteCustomerRequest capturedRequest = null;

            var inviteCustomerServiceMock = new Mock<IInviteCustomerService>();
            inviteCustomerServiceMock
                .Setup(s => s.InviteCustomerAsyc(It.IsAny<InviteCustomerRequest>(), It.IsAny<CancellationToken>()))
                .Callback<InviteCustomerRequest, CancellationToken>((request, _) => capturedRequest = request)
                .ReturnsAsync(new InviteCustomerResult { Succeeded = true, Errors = [] });

            var handler = new InviteUserCommandHandler(inviteCustomerServiceMock.Object);

            var command = new InviteUserCommand
            {
                StoreId = "store1",
                OrganizationId = "org1",
                UrlSuffix = "/confirm-invitation",
                Emails = ["existing@test.com"],
                Message = "Welcome!",
                RoleIds = ["org-employee"],
                CustomerOrderId = "order1",
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Succeeded);
            Assert.Empty(result.Errors);

            Assert.NotNull(capturedRequest);
            Assert.Equal(command.StoreId, capturedRequest.StoreId);
            Assert.Equal(command.OrganizationId, capturedRequest.OrganizationId);
            Assert.Equal(command.UrlSuffix, capturedRequest.UrlSuffix);
            Assert.Equal(command.Emails, capturedRequest.Emails);
            Assert.Equal(command.Message, capturedRequest.Message);
            Assert.Equal(command.RoleIds, capturedRequest.RoleIds);
            Assert.Equal(command.CustomerOrderId, capturedRequest.AdditionalParameters["customerOrderId"]);
        }

        [Fact]
        public async Task Handle_MapsInviteCustomerErrors_ToIdentityErrorInfo()
        {
            // Arrange
            var inviteCustomerServiceMock = new Mock<IInviteCustomerService>();
            inviteCustomerServiceMock
                .Setup(s => s.InviteCustomerAsyc(It.IsAny<InviteCustomerRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new InviteCustomerResult
                {
                    Succeeded = false,
                    Errors =
                    [
                        new()
                        {
                            Code = "AlreadyMemberOfOrganization",
                            Description = "User with email 'existing@test.com' is already a member of organization 'org1'",
                            Parameter = "org1",
                            Email = "existing@test.com",
                        },
                    ],
                });

            var handler = new InviteUserCommandHandler(inviteCustomerServiceMock.Object);

            var command = new InviteUserCommand
            {
                StoreId = "store1",
                OrganizationId = "org1",
                Emails = ["existing@test.com"],
                RoleIds = ["org-employee"],
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Succeeded);
            var error = Assert.Single(result.Errors);
            Assert.Equal("AlreadyMemberOfOrganization", error.Code);
            Assert.Equal("org1", error.Parameter);
        }
    }
}

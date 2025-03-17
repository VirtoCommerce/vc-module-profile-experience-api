using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.NotificationsModule.Core.Model;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers
{
    public class RequestPasswordResetQueryHandlerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<INotificationSearchService> _notificationSearchServiceMock;
        private readonly Mock<INotificationSender> _notificationSenderMock;
        private readonly Mock<IStoreService> _storeServiceMock;
        private readonly RequestPasswordResetQueryHandler _handler;

        public RequestPasswordResetQueryHandlerTests()
        {
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            _notificationSearchServiceMock = new Mock<INotificationSearchService>();
            _notificationSenderMock = new Mock<INotificationSender>();
            _storeServiceMock = new Mock<IStoreService>();

            _handler = new RequestPasswordResetQueryHandler(
                () => _userManagerMock.Object,
                _notificationSearchServiceMock.Object,
                _notificationSenderMock.Object,
                _storeServiceMock.Object);
        }

        [Fact]
        public async Task Handle_UserWithNullLockoutEnd_ScheduleSendNotificationAsyncCalled()
        {
            // Arrange
            var user = CreateUser(null);
            var store = CreateStore();
            var notification = new ResetPasswordEmailNotification();

            SetupMocks(user, store, notification);

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = "test@example.com",
                UrlSuffix = "/reset-password"
            };

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithExpiredLockoutEnd_ScheduleSendNotificationAsyncCalled()
        {
            // Arrange
            var user = CreateUser(DateTime.UtcNow.AddHours(-1));
            var store = CreateStore();
            var notification = new ResetPasswordEmailNotification();

            SetupMocks(user, store, notification);

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = "test@example.com",
                UrlSuffix = "/reset-password"
            };

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithFutureLockoutEnd_ScheduleSendNotificationAsyncNotCalled()
        {
            // Arrange
            var user = CreateUser(DateTime.UtcNow.AddHours(1));
            var store = CreateStore();
            var notification = new ResetPasswordEmailNotification();

            SetupMocks(user, store, notification);

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = "test@example.com",
                UrlSuffix = "/reset-password"
            };

            // Act
            await _handler.Handle(request, CancellationToken.None);

            // Assert
            _notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), Times.Never);
        }

        private static ApplicationUser CreateUser(DateTime? lockoutEnd)
        {
            return new ApplicationUser
            {
                Id = "testUserId",
                Email = "test@example.com",
                StoreId = "testStoreId",
                LockoutEnd = lockoutEnd
            };
        }

        private static Store CreateStore()
        {
            return new Store
            {
                Id = "testStoreId",
                Url = "http://teststore.com",
                Email = "store@example.com"
            };
        }

        private void SetupMocks(ApplicationUser user, Store store, ResetPasswordEmailNotification notification)
        {
            _userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("testToken");
            _storeServiceMock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new List<Store> { store });
            _notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>())).ReturnsAsync(new NotificationSearchResult { Results = new List<Notification> { notification }, TotalCount = 1 });
        }
    }
}

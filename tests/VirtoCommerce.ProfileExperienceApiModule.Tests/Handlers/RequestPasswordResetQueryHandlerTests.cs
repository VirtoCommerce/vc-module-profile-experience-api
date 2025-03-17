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
        [Fact]
        public async Task Handle_UserWithNullLockoutEnd_ScheduleSendNotificationAsyncCalled()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var notificationSearchServiceMock = new Mock<INotificationSearchService>();
            var notificationSenderMock = new Mock<INotificationSender>();
            var storeServiceMock = new Mock<IStoreService>();

            var user = new ApplicationUser
            {
                Id = "testUserId",
                Email = "test@example.com",
                StoreId = "testStoreId",
                LockoutEnd = null
            };

            var store = new Store
            {
                Id = "testStoreId",
                Url = "http://teststore.com",
                Email = "store@example.com"
            };

            var notification = new ResetPasswordEmailNotification();

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("testToken");
            storeServiceMock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync([store]);
            notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>())).ReturnsAsync(new NotificationSearchResult { Results = [notification], TotalCount = 1 });

            var handler = new RequestPasswordResetQueryHandler(
                () => userManagerMock.Object,
                notificationSearchServiceMock.Object,
                notificationSenderMock.Object,
                storeServiceMock.Object);

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = "test@example.com",
                UrlSuffix = "/reset-password"
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UserWithExpiredLockoutEnd_ScheduleSendNotificationAsyncCalled()
        {
            // Arrange
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            var notificationSearchServiceMock = new Mock<INotificationSearchService>();
            var notificationSenderMock = new Mock<INotificationSender>();
            var storeServiceMock = new Mock<IStoreService>();

            var user = new ApplicationUser
            {
                Id = "testUserId",
                Email = "test@example.com",
                StoreId = "testStoreId",
                LockoutEnd = DateTime.UtcNow.AddHours(-1)
            };

            var store = new Store
            {
                Id = "testStoreId",
                Url = "http://teststore.com",
                Email = "store@example.com"
            };

            var notification = new ResetPasswordEmailNotification();

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(user);
            userManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("testToken");
            storeServiceMock.Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync([store]);
            notificationSearchServiceMock.Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>())).ReturnsAsync(new NotificationSearchResult { Results = [notification], TotalCount = 1 });

            var handler = new RequestPasswordResetQueryHandler(
                () => userManagerMock.Object,
                notificationSearchServiceMock.Object,
                notificationSenderMock.Object,
                storeServiceMock.Object);

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = "test@example.com",
                UrlSuffix = "/reset-password"
            };

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), Times.Once);
        }
    }
}

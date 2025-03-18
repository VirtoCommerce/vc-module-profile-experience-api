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
        [Theory]
        [InlineData(null, true)]
        [InlineData(-1, true)]
        [InlineData(1, false)]
        public async Task WhenLockoutEndIsNotInFuture_SendNotification(int? lockoutEndHours, bool expectedNotification)
        {
            // Arrange
            var store = new Store
            {
                Id = "testStoreId",
                Url = "http://teststore.com",
                Email = "store@example.com",
            };

            var user = new ApplicationUser
            {
                Id = "testUserId",
                Email = "test@example.com",
                StoreId = store.Id,
                LockoutEnd = lockoutEndHours is null
                    ? null
                    : DateTime.UtcNow.AddHours(lockoutEndHours.Value),
            };

            var request = new RequestPasswordResetQuery
            {
                LoginOrEmail = user.Email,
            };

            var notificationSenderMock = new Mock<INotificationSender>();
            var handler = GetHandler(notificationSenderMock.Object, store, user);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            var expectedTimes = expectedNotification ? Times.Once() : Times.Never();
            notificationSenderMock.Verify(x => x.ScheduleSendNotificationAsync(It.IsAny<ResetPasswordEmailNotification>()), expectedTimes);
        }


        private static RequestPasswordResetQueryHandler GetHandler(INotificationSender notificationSender, Store store, ApplicationUser user)
        {
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            userManagerMock
                .Setup(x => x.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("testToken");

            userManagerMock
                .Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            var notificationSearchServiceMock = new Mock<INotificationSearchService>();

            notificationSearchServiceMock
                .Setup(x => x.SearchNotificationsAsync(It.IsAny<NotificationSearchCriteria>()))
                .ReturnsAsync(new NotificationSearchResult
                {
                    Results = [new ResetPasswordEmailNotification()],
                    TotalCount = 1,
                });

            var storeServiceMock = new Mock<IStoreService>();

            storeServiceMock
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([store]);

            return new RequestPasswordResetQueryHandler(
                () => userManagerMock.Object,
                notificationSearchServiceMock.Object,
                notificationSender,
                storeServiceMock.Object);
        }
    }
}

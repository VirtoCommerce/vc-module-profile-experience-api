using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.NotificationsModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.Xapi.Tests.Helpers;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class RegisterRequestCommandHandlerTests : MoqHelper
{
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly Mock<IDynamicPropertyUpdaterService> _dynamicPropertyUpdaterMock = new();
    private readonly Mock<IMemberService> _memberServiceMock = new();
    private readonly Mock<IStoreService> _storeServiceMock = new();
    private readonly Mock<INotificationSearchService> _notificationSearchServiceMock = new();
    private readonly Mock<INotificationSender> _notificationSenderMock = new();
    private readonly Mock<IAccountService> _accountServiceMock = new();
    private readonly Mock<IOrganizationMembershipService> _membershipServiceMock = new();

    [Fact]
    public async Task CreateOrganizationMembershipAsync_SavesMembershipWithCorrectData()
    {
        // Arrange
        var handler = BuildExposedHandler();
        var account = new ApplicationUser { Id = "user-1" };
        var role = new Role { Id = "role-1", Name = "org-maintainer" };

        // Act
        await handler.InvokeCreateOrganizationMembershipAsync(account, "org-1", role);

        // Assert
        _membershipServiceMock.Verify(
            x => x.SaveChangesAsync(It.Is<IList<OrganizationMembership>>(list =>
                list.Count == 1 &&
                list[0].UserId == "user-1" &&
                list[0].OrganizationId == "org-1" &&
                list[0].Roles.Count == 1 &&
                list[0].Roles[0].RoleId == "role-1" &&
                list[0].Roles[0].RoleName == "org-maintainer")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithOrganization_CreatesMembershipAndDoesNotAssignGlobalRoleToAccount()
    {
        // Arrange
        SetupCommonMocks();
        var handler = BuildCapturingHandler();
        var command = BuildCommand(withOrganization: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — membership is created for the org, and the account itself has no global roles
        Assert.True(result.AccountCreationResult?.Succeeded);
        Assert.True(handler.CreateMembershipCalled);
        Assert.Equal("org-1", handler.CapturedOrganizationId);
        Assert.Equal("r-maintainer", handler.CapturedRole?.Id);
        Assert.True(
            handler.CapturedAccount?.Roles == null || handler.CapturedAccount.Roles.Count == 0,
            "MaintainerRole must not be assigned globally to the account");
    }

    [Fact]
    public async Task Handle_WithoutOrganization_DoesNotCreateMembership()
    {
        // Arrange
        SetupCommonMocks();
        var handler = BuildCapturingHandler();
        var command = BuildCommand(withOrganization: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.AccountCreationResult?.Succeeded);
        Assert.False(handler.CreateMembershipCalled);
    }

    private void SetupCommonMocks()
    {
        _mapperMock
            .Setup(m => m.Map<Organization>(It.IsAny<object>()))
            .Returns(new Organization { Id = "org-1", Name = "Test Org" });

        _mapperMock
            .Setup(m => m.Map<Contact>(It.IsAny<object>()))
            .Returns(new Contact { FirstName = "John", LastName = "Doe" });

        _memberServiceMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<Member[]>()))
            .Returns(Task.CompletedTask);

        _accountServiceMock
            .Setup(x => x.CreateAccountAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => u.Id = "user-1")
            .ReturnsAsync(IdentityResult.Success);
    }

    private static RegisterRequestCommand BuildCommand(bool withOrganization) => new()
    {
        StoreId = "store-1",
        LanguageCode = "en-US",
        Account = new Account { UserName = "john.doe", Email = "john@example.com", Password = "Test1234!" },
        Contact = new RegisteredContact { FirstName = "John", LastName = "Doe" },
        Organization = withOrganization ? new RegisteredOrganization { Name = "Test Org" } : null,
    };

    private static IOptions<InputValidationOptions> DisabledValidationOptions() =>
        Options.Create(new InputValidationOptions
        {
            NameValidationPattern = null,
            EnableNoHtmlTagsValidation = false,
            EnableScriptInjectionValidation = false,
        });

    private ExposedHandler BuildExposedHandler()
    {
        var opts = DisabledValidationOptions();
        return new ExposedHandler(
            _mapperMock.Object, _dynamicPropertyUpdaterMock.Object,
            _memberServiceMock.Object, _storeServiceMock.Object,
            _notificationSearchServiceMock.Object, _notificationSenderMock.Object,
            _accountServiceMock.Object,
            new NewContactValidator(opts), new AccountValidator(opts),
            new AddressValidator(opts), new OrganizationValidator(opts),
            Options.Create(new FrontendSecurityOptions { OrganizationMaintainerRole = "org-maintainer" }),
            _mediatorMock.Object, _membershipServiceMock.Object);
    }

    private CapturingHandler BuildCapturingHandler()
    {
        var opts = DisabledValidationOptions();
        return new CapturingHandler(
            _mapperMock.Object, _dynamicPropertyUpdaterMock.Object,
            _memberServiceMock.Object, _storeServiceMock.Object,
            _notificationSearchServiceMock.Object, _notificationSenderMock.Object,
            _accountServiceMock.Object,
            new NewContactValidator(opts), new AccountValidator(opts),
            new AddressValidator(opts), new OrganizationValidator(opts),
            Options.Create(new FrontendSecurityOptions { OrganizationMaintainerRole = "org-maintainer" }),
            _mediatorMock.Object, _membershipServiceMock.Object);
    }

    private sealed class ExposedHandler(
        IMapper mapper, IDynamicPropertyUpdaterService dynamicPropertyUpdater,
        IMemberService memberService, IStoreService storeService,
        INotificationSearchService notificationSearchService, INotificationSender notificationSender,
        IAccountService accountService,
        NewContactValidator contactValidator, AccountValidator accountValidator,
        AddressValidator addressValidator, OrganizationValidator organizationValidator,
        IOptions<FrontendSecurityOptions> securityOptions, IMediator mediator,
        IOrganizationMembershipService organizationMembershipService)
        : RegisterRequestCommandHandler(mapper, dynamicPropertyUpdater, memberService, storeService,
            notificationSearchService, notificationSender, accountService,
            contactValidator, accountValidator, addressValidator, organizationValidator,
            securityOptions, mediator, organizationMembershipService)
    {
        public Task InvokeCreateOrganizationMembershipAsync(ApplicationUser account, string organizationId, Role role)
            => CreateOrganizationMembershipAsync(account, organizationId, role);
    }

    private sealed class CapturingHandler(
        IMapper mapper, IDynamicPropertyUpdaterService dynamicPropertyUpdater,
        IMemberService memberService, IStoreService storeService,
        INotificationSearchService notificationSearchService, INotificationSender notificationSender,
        IAccountService accountService,
        NewContactValidator contactValidator, AccountValidator accountValidator,
        AddressValidator addressValidator, OrganizationValidator organizationValidator,
        IOptions<FrontendSecurityOptions> securityOptions, IMediator mediator,
        IOrganizationMembershipService organizationMembershipService)
        : RegisterRequestCommandHandler(mapper, dynamicPropertyUpdater, memberService, storeService,
            notificationSearchService, notificationSender, accountService,
            contactValidator, accountValidator, addressValidator, organizationValidator,
            securityOptions, mediator, organizationMembershipService)
    {
        public bool CreateMembershipCalled { get; private set; }
        public ApplicationUser CapturedAccount { get; private set; }
        public string CapturedOrganizationId { get; private set; }
        public Role CapturedRole { get; private set; }

        protected override Task BeforeProcessRequestAsync(
            RegisterRequestCommand request,
            RegisterOrganizationResult result,
            CancellationTokenSource tokenSource)
        {
            // Bypass store/role lookup. Protected properties with private setters require reflection.
            SetProtected("CurrentStore", new Store { Id = "store-1", DefaultCurrency = "USD", DefaultLanguage = "en-US" });
            SetProtected("EmailVerificationFlow", "");
            SetProtected("DefaultContactStatus", "Active");
            SetProtected("DefaultOrganizationStatus", "Active");
            SetProtected("MaintainerRole", new Role { Id = "r-maintainer", Name = "org-maintainer" });
            return Task.CompletedTask;
        }

        protected override Task CreateOrganizationMembershipAsync(ApplicationUser account, string organizationId, Role role)
        {
            CreateMembershipCalled = true;
            CapturedAccount = account;
            CapturedOrganizationId = organizationId;
            CapturedRole = role;
            return Task.CompletedTask;
        }

        private void SetProtected(string name, object value) =>
            typeof(RegisterRequestCommandHandler)
                .GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!
                .SetValue(this, value);
    }
}

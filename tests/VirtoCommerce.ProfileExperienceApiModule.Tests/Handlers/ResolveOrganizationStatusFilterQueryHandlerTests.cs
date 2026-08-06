using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using VirtoCommerce.CustomerModule.Core;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Handlers;

public class ResolveOrganizationStatusFilterQueryHandlerTests
{
    private const string OrgId = "org-1";

    private readonly Mock<IMemberSearchService> _memberSearchServiceMock = new();
    private readonly Mock<IOrganizationMembershipSearchService> _membershipSearchServiceMock = new();

    private ResolveOrganizationStatusFilterQueryHandler BuildHandler() =>
        new(_memberSearchServiceMock.Object, _membershipSearchServiceMock.Object);

    private void SetupSearches(IList<Contact> contacts, IList<OrganizationMembership> memberships)
    {
        _memberSearchServiceMock
            .Setup(x => x.SearchAllAsync(It.IsAny<MembersSearchCriteria>()))
            .ReturnsAsync(contacts.Cast<Member>().ToList());

        _membershipSearchServiceMock
            .Setup(x => x.SearchAsync(It.IsAny<OrganizationMembershipSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync(new OrganizationMembershipSearchResult { Results = memberships, TotalCount = memberships.Count });
    }

    [Fact]
    public async Task Handle_NoMembership_UsesContactGlobalStatus()
    {
        // Arrange - no OrganizationMembership row for this contact's user, so the effective status
        // falls back to the contact's own global status (ResolveEffectiveStatus semantics)
        var contact = new Contact
        {
            Id = "contact-1",
            Status = ModuleConstants.MembershipStatuses.Approved,
            SecurityAccounts = [new ApplicationUser { Id = "user-1" }],
        };
        SetupSearches([contact], []);

        var handler = BuildHandler();
        var query = new ResolveOrganizationStatusFilterQuery { OrganizationId = OrgId, Statuses = [ModuleConstants.MembershipStatuses.Approved] };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.FilterRequired);
        Assert.Contains("contact-1", result.Ids);
    }

    [Fact]
    public async Task Handle_MembershipStatusOverridesContactStatus()
    {
        // Arrange - membership has its own status override that differs from the contact's global one
        var contact = new Contact
        {
            Id = "contact-1",
            Status = ModuleConstants.MembershipStatuses.Rejected,
            SecurityAccounts = [new ApplicationUser { Id = "user-1" }],
        };
        var membership = new OrganizationMembership { UserId = "user-1", Status = ModuleConstants.MembershipStatuses.Approved };
        SetupSearches([contact], [membership]);

        var handler = BuildHandler();
        var query = new ResolveOrganizationStatusFilterQuery { OrganizationId = OrgId, Statuses = [ModuleConstants.MembershipStatuses.Approved] };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert - matches the membership's Approved override, not the contact's Rejected global status
        Assert.Contains("contact-1", result.Ids);
    }

    [Fact]
    public async Task Handle_LockedFilter_MatchesOnlyCurrentlyLockedMemberships()
    {
        // Arrange - one locked, one unlocked-but-otherwise-matching-status membership
        var lockedContact = new Contact { Id = "contact-locked", SecurityAccounts = [new ApplicationUser { Id = "user-locked" }] };
        var unlockedContact = new Contact
        {
            Id = "contact-unlocked",
            Status = ModuleConstants.MembershipStatuses.Approved,
            SecurityAccounts = [new ApplicationUser { Id = "user-unlocked" }],
        };
        var lockedMembership = new OrganizationMembership { UserId = "user-locked", IsLocked = true };
        SetupSearches([lockedContact, unlockedContact], [lockedMembership]);

        var handler = BuildHandler();
        var query = new ResolveOrganizationStatusFilterQuery { OrganizationId = OrgId, Statuses = ["Locked"] };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Contains("contact-locked", result.Ids);
        Assert.DoesNotContain("contact-unlocked", result.Ids);
    }
}

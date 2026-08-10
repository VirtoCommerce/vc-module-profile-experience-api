using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class ResolveOrganizationStatusFilterQueryHandler : IQueryHandler<ResolveOrganizationStatusFilterQuery, ContactIdFilterResult>
{
    private const string LockedFilterValue = "Locked";

    private readonly IMemberSearchService _memberSearchService;
    private readonly IOrganizationMembershipSearchService _organizationMembershipService;

    public ResolveOrganizationStatusFilterQueryHandler(
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService)
    {
        _memberSearchService = memberSearchService;
        _organizationMembershipService = organizationMembershipService;
    }

    public virtual async Task<ContactIdFilterResult> Handle(ResolveOrganizationStatusFilterQuery request, CancellationToken cancellationToken)
    {
        var orgId = request.OrganizationId;
        var statuses = request.Statuses;

        var membershipsTask = _organizationMembershipService.SearchAllNoCloneAsync(
            new OrganizationMembershipSearchCriteria { OrganizationId = orgId });
        var contactsTask = _memberSearchService.SearchAllAsync(
            new MembersSearchCriteria { MemberId = orgId });

        await Task.WhenAll(membershipsTask, contactsTask);

        var membershipByUserId = membershipsTask.Result
            .GroupBy(m => m.UserId)
            .ToDictionary(g => g.Key, g => g.First());

        var wantsLocked = statuses.Contains(LockedFilterValue);
        var lifecycleStatuses = statuses.Where(s => s != LockedFilterValue).ToHashSet();

        var qualifyingContactIds = contactsTask.Result
            .Where(contact => ContactMatchesStatusFilter(contact, membershipByUserId, wantsLocked, lifecycleStatuses))
            .Select(contact => contact.Id)
            .ToList();

        return new ContactIdFilterResult { FilterRequired = true, Ids = qualifyingContactIds };
    }

    private static bool ContactMatchesStatusFilter(
        Member contact, IDictionary<string, OrganizationMembership> membershipByUserId, bool wantsLocked, HashSet<string> lifecycleStatuses)
    {
        var membership = FindMembership(contact, membershipByUserId);

        if (membership?.IsCurrentlyLocked == true)
        {
            return wantsLocked;
        }

        if (lifecycleStatuses.Count == 0)
        {
            return false;
        }

        var effectiveStatus = OrganizationMembership.ResolveEffectiveStatus(membership?.Status, contact.Status);

        return !string.IsNullOrEmpty(effectiveStatus) && lifecycleStatuses.Contains(effectiveStatus);
    }

    private static OrganizationMembership FindMembership(Member contact, IDictionary<string, OrganizationMembership> membershipByUserId)
    {
        var securityAccountIds = (contact as IHasSecurityAccounts)?.SecurityAccounts?
            .Select(sa => sa.Id)
            .Where(id => !string.IsNullOrEmpty(id)) ?? [];

        foreach (var securityAccountId in securityAccountIds)
        {
            if (membershipByUserId.TryGetValue(securityAccountId, out var membership))
            {
                return membership;
            }
        }

        return null;
    }
}

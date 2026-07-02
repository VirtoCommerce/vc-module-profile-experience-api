using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SearchMembersQueryHandler :
        IRequestHandler<SearchContactsQuery, MemberSearchResult>,
        IRequestHandler<SearchOrganizationsQuery, MemberSearchResult>
    {
        private readonly IMemberSearchService _memberSearchService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;

        public SearchMembersQueryHandler(
            IMemberSearchService memberSearchService,
            IOrganizationMembershipSearchService organizationMembershipSearchService)
        {
            _memberSearchService = memberSearchService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
        }

        public virtual Task<MemberSearchResult> Handle(SearchContactsQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = BuildMembersSearchCriteria(request, nameof(Contact));

            return _memberSearchService.SearchMembersAsync(searchCriteria);
        }

        public virtual async Task<MemberSearchResult> Handle(SearchOrganizationsQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = BuildMembersSearchCriteria(request, nameof(Organization));
            var result = await _memberSearchService.SearchMembersAsync(searchCriteria);

            if (!string.IsNullOrEmpty(request.UserId) && result.Results.Count > 0)
            {
                result = await FilterLockedOrganizationsAsync(result, request.UserId);
            }

            return result;
        }

        protected virtual MembersSearchCriteria BuildMembersSearchCriteria(SearchMembersQueryBase request, string memberType)
        {
            var result = AbstractTypeFactory<MembersSearchCriteria>.TryCreateInstance();
            result.DeepSearch = request.DeepSearch;
            result.MemberType = memberType;
            result.Keyword = request.Keyword;
            result.Skip = request.Skip;
            result.Take = request.Take;
            result.Sort = request.Sort;
            result.ObjectIds = request.ObjectIds;
            result.MemberId = request.MemberId;

            return result;
        }

        private async Task<MemberSearchResult> FilterLockedOrganizationsAsync(MemberSearchResult result, string userId)
        {
            var lockedOrgIds = await _organizationMembershipSearchService.GetLockedOrganizationIdsAsync(userId);

            if (lockedOrgIds.Count == 0)
            {
                return result;
            }

            var lockedSet = new HashSet<string>(lockedOrgIds, StringComparer.OrdinalIgnoreCase);

            var filtered = result.Results
                .Where(m => !lockedSet.Contains(m.Id))
                .ToList();

            return new MemberSearchResult
            {
                Results = filtered,
                TotalCount = result.TotalCount - (result.Results.Count - filtered.Count),
            };
        }
    }
}

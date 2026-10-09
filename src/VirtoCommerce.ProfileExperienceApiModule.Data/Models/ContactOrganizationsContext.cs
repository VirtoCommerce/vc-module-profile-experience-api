using System.Collections.Generic;
using System.Security.Claims;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models;

/// <summary>
/// Pipeline context to narrow ContactType organizations lists: organizationsIds and organizations fields.
/// </summary>
public class ContactOrganizationsContext
{
    public ContactAggregate Contact { get; set; }

    public ClaimsPrincipal Principal { get; set; }

    public string CurrentOrganizationId { get; set; }

    public IList<string> Statuses { get; set; }

    /// <summary>
    /// Organization ids after filtering by statuses.
    /// </summary>
    public IReadOnlyList<string> SourceOrganizationIds { get; set; } = [];

    /// <summary>
    /// Organization ids to return. Initialized with SourceOrganizationIds. Middlewares may replace or modify this list.
    /// </summary>
    public IList<string> DestinationOrganizationIds { get; set; } = [];
}

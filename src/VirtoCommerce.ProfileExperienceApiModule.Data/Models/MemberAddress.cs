using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models;

public class MemberAddress : Address
{
    public string Id { get => Key; set => Key = value; }

    public bool IsFavorite { get; set; }
}

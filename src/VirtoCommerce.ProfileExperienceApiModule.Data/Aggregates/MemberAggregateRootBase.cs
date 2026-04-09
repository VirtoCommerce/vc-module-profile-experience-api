using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public abstract class MemberAggregateRootBase : IMemberAggregateRoot
    {
        public virtual Member Member { get; set; }

        private static readonly IEqualityComparer<Address> _addressComparer = AnonymousComparer.Create((Address x) => new
        {
            x.FirstName,
            x.LastName,
            x.City,
            x.Line1,
            x.Line2,
            x.CountryCode,
            x.RegionId,
            x.PostalCode,
            x.Phone,
            x.Email,
        });

        public virtual MemberAggregateRootBase UpdateAddresses(IList<Address> addresses)
        {
            foreach (var address in addresses)
            {
                var addressForReplacement = Member.Addresses.FirstOrDefault(x => x.Key == address.Key);

                if (addressForReplacement != null)
                {
                    var index = Member.Addresses.IndexOf(addressForReplacement);
                    Member.Addresses[index] = address;
                }
                else
                {
                    // If we are adding new entry, we shouldn't manage the ids.
                    address.Key = null;

                    if (!IsDuplicateAddress(address))
                    {
                        Member.Addresses.Add(address);
                    }
                }
            }

            return this;
        }

        public virtual bool IsDuplicateAddress(Address address)
        {
            return Member.Addresses.Any(x => _addressComparer.Equals(x, address));
        }

        public virtual MemberAggregateRootBase DeleteAddresses(IList<Address> addresses)
        {
            foreach (var address in addresses)
            {
                var addressToDelete = Member.Addresses.FirstOrDefault(x => x.Key == address.Key);

                if (addressToDelete != null)
                {
                    Member.Addresses.Remove(addressToDelete);
                }
            }

            return this;
        }
    }
}

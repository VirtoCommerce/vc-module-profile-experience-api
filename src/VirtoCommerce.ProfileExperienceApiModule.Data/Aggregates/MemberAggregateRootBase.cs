using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public abstract class MemberAggregateRootBase : IMemberAggregateRoot
    {
        public virtual Member Member { get; set; }

        public virtual MemberAggregateRootBase UpdateAddresses(IList<Address> addresses)
        {
            foreach (var address in addresses)
            {
                if (address != null && address.AddressType == CoreModule.Core.Common.AddressType.Undefined)
                {
                    address.AddressType = CoreModule.Core.Common.AddressType.BillingAndShipping;
                }

                var addressForReplacement = Member.Addresses.FirstOrDefault(x => x.Key == address.Key);

                if (addressForReplacement != null)
                {
                    var index = Member.Addresses.IndexOf(addressForReplacement);
                    Member.Addresses[index] = address;
                }
                else
                {
                    // If we adding new entry, we shouldn't manage the ids.
                    address.Key = null;
                    Member.Addresses.Add(address);
                }
            }

            return this;
        }

        public virtual MemberAggregateRootBase DeleteAddresses(IList<Address> addresses)
        {
            foreach (var removedItem in Member.Addresses.Intersect(addresses).ToArray())
            {
                Member.Addresses.Remove(removedItem);
            }

            return this;
        }
    }
}

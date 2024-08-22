using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.CustomerModule.Core.Model;
using static VirtoCommerce.CoreModule.Core.Common.AddressType;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public abstract class MemberAggregateRootBase : IMemberAggregateRoot
    {
        public virtual Member Member { get; set; }

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
                    // If we adding new entry, we shouldn't manage the ids.
                    address.Key = null;
                    Member.Addresses.Add(address);
                }
            }

            return this;
        }

        public virtual MemberAggregateRootBase DeleteAddresses(IList<Address> addresses)
        {
            foreach (var address in addresses)
            {
                var addressToDelete = Member.Addresses.FirstOrDefault(x => x.Key == address.Key);

                if (addressToDelete != null && addressToDelete.AddressType != BillingAndShipping)
                {
                    Member.Addresses.Remove(addressToDelete);
                }
            }

            return this;
        }

        public virtual MemberAggregateRootBase MakeAddressDefault(string addressKey)
        {
            var addressToDefault = Member.Addresses.FirstOrDefault(x => x.Key == addressKey);

            if (addressToDefault != null)
            {
                foreach (var address in Member.Addresses.Where(x => x.AddressType == addressToDefault.AddressType))
                {
                    address.IsDefault = false;
                }

                addressToDefault.IsDefault = true;
            }

            return this;
        }
    }
}

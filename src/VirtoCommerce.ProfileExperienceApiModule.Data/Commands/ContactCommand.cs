using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public abstract class ContactCommand : ICommand<ContactAggregate>
    {
        protected ContactCommand()
        {
            MemberType = nameof(Contact);
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string MemberType { get; set; }
        public string PhotoUrl { get; set; }
        public string TimeZone { get; set; }
        public string DefaultLanguage { get; set; }
        public string CurrencyCode { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public string Salutation { get; set; }
        public string About { get; set; }
        public string SelectedAddressId { get; set; }
        public IList<Address> Addresses { get; set; }
        public IList<string> Phones { get; set; }
        public IList<string> Emails { get; set; }
        public IList<string> Groups { get; set; }
        public IList<string> Organizations { get; set; }
        public IList<DynamicPropertyValue> DynamicProperties { get; set; }

        public Address DefaultBillingAddress { get; set; }
        public Address DefaultShippingAddress { get; set; }

        /// <summary>
        /// Returns true if the customer accepts marketing, returns false if the customer does not.
        /// </summary>
        public bool AcceptsMarketing { get; set; }

        /// <summary>
        /// Returns the default customer_address.
        /// </summary>
        public Address DefaultAddress { get; set; }


        public IList<string> PhoneNumbers
        {
            get
            {
                return Phones;
            }
            set
            {
                Phones = value;
            }
        }


        /// <summary>
        /// User groups such as VIP, Wholesaler etc
        /// </summary>
        public IList<string> UserGroups
        {
            get
            {
                return Groups;
            }
            set
            {
                Groups = value;
            }
        }
    }
}

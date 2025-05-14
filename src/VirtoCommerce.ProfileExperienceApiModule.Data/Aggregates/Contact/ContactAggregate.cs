using System.Linq;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact
{
    public class ContactAggregate : MemberAggregateRootBase
    {
        public CustomerModule.Core.Model.Contact Contact => Member as CustomerModule.Core.Model.Contact;
        public string StoreId { get; set; }

        public override Member Member
        {
            get => base.Member;
            set
            {
                base.Member = value;

                if (Contact != null && string.IsNullOrEmpty(Contact.FullName))
                {
                    Contact.FullName = string.Join(" ", Contact.FirstName, Contact.LastName);
                }

                StoreId = Contact?.SecurityAccounts?.Select(x => x.StoreId).FirstOrDefault();
            }
        }

        public virtual ContactAggregate UpdatePersonalDetails(PersonalData personalDetails)
        {
            Contact.FirstName = personalDetails.FirstName;
            Contact.LastName = personalDetails.LastName;
            Contact.MiddleName = personalDetails.MiddleName;
            Contact.FullName = personalDetails.FullName;

            return this;
        }
    }
}

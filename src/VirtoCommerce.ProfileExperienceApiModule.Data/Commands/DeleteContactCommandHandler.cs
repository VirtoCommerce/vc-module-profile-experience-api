using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class DeleteContactCommandHandler : IRequestHandler<DeleteContactCommand, bool>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        public DeleteContactCommandHandler(IContactAggregateRepository contactAggregateRepository)
        {
            _contactAggregateRepository = contactAggregateRepository;
        }
        public virtual async Task<bool> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
        {
            await _contactAggregateRepository.DeleteAsync(request.ContactId);

            return true;
        }
    }
}

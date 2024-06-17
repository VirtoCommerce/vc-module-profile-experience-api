using AutoFixture;
using GraphQL.DataLoader;
using MediatR;
using Moq;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CoreModule.Core.Currency;

namespace VirtoCommerce.Xapi.Tests.Helpers
{
    public class MoqHelper
    {
        protected readonly Fixture _fixture = new Fixture();

        protected const string CURRENCY_CODE = "USD";
        protected const string CULTURE_NAME = "en-US";
        protected const string DEFAULT_STORE_ID = "default";

        protected readonly Mock<IMediator> _mediatorMock = new Mock<IMediator>();
        protected readonly Mock<IDataLoaderContextAccessor> _dataLoaderContextAccessorMock = new Mock<IDataLoaderContextAccessor>();

        public MoqHelper()
        {
            _fixture.Register(() => new Language(CULTURE_NAME));
            _fixture.Register(() => new Currency(_fixture.Create<Language>(), CURRENCY_CODE)
            {
                RoundingPolicy = new DefaultMoneyRoundingPolicy()
            });
        }

        protected Discount GetDiscount() => _fixture.Create<Discount>();

        protected Currency GetCurrency() => _fixture.Create<Currency>();

        protected Money GetMoney(decimal? amount = null) => new Money(amount ?? _fixture.Create<decimal>(), GetCurrency());
    }
}

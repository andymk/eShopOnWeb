using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces
{
    public class RavenDbBasketService : IBasketService
    {
        private readonly IAsyncDocumentSession _session;

        public RavenDbBasketService(IDocumentStore store)
        {
            _session = store.OpenAsyncSession();
        }

        public async Task<int> GetBasketItemCountAsync(string userName)
        {
            var items = await _session.Query<Basket>()
                .Where(x => x.BuyerId == userName)
                .Select(x => x.Items.Sum(i => i.Quantity))
                .FirstOrDefaultAsync();

            return items;
        }

        public async Task TransferBasketAsync(string anonymousId, string userName)
        {
            var basket = await _session.Query<Basket>()
                .FirstAsync(x => x.BuyerId == anonymousId);
            basket.SetNewBuyerId(userName);
            await _session.SaveChangesAsync();
        }

        public async Task AddItemToBasket(int basketId, int catalogItemId, decimal price, int quantity = 1)
        {
            var basket = await _session.LoadAsync<Basket>("baskets/" + basketId);
            basket.AddItem(catalogItemId, price,quantity);
            await _session.SaveChangesAsync();
        }

        public async Task SetQuantities(int basketId, Dictionary<string, int> quantities)
        {
            var basket = await _session.LoadAsync<Basket>("baskets/" + basketId);
            foreach (var item in basket.Items)
            {
                if (quantities.TryGetValue(item.Id.ToString(), out var quantity))
                {
                    item.SetNewQuantity(quantity);
                }
            }
            basket.RemoveEmptyItems();
            await _session.SaveChangesAsync();
        }

        public async Task DeleteBasketAsync(int basketId)
        {
            _session.Delete("Baskets/" + basketId);
            await _session.SaveChangesAsync();
        }
    }
}
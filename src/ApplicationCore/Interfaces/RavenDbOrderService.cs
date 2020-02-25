using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Microsoft.eShopWeb.ApplicationCore.Interfaces
{
    public class RavenDbOrderService : IOrderService
    {
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncDocumentSession _session;

        public RavenDbOrderService(IDocumentStore store, IUriComposer uriComposer)
        {
            _uriComposer = uriComposer;
            _session = store.OpenAsyncSession();
        }
        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basket = await _session.LoadAsync<Basket>("Baskets/" + basketId,
                x => x.IncludeDocuments(b=>b.Items.Select(i=>i.CatalogItem)));
            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                // no server call here
                var catalogItem = await _session.LoadAsync<CatalogItem>(item.CatalogItem);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, 
                    _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, item.UnitPrice, item.Quantity);
                items.Add(orderItem);
            }
            
            var order = new Order(basket.BuyerId, shippingAddress, items);
            await _session.StoreAsync(order);
            await _session.SaveChangesAsync();
        }
    }
}
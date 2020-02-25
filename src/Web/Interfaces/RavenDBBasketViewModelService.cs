using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Web.Pages.Basket;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Microsoft.eShopWeb.Web.Interfaces
{
    public class RavenDbBasketViewModelService : IBasketViewModelService
    {
        private readonly IAsyncDocumentSession _session;
        private readonly IUriComposer _uriComposer;
        public RavenDbBasketViewModelService(IDocumentStore store, IUriComposer uriComposer)
        {
            _uriComposer = uriComposer;
            _session = store.OpenAsyncSession();
        }

        public async Task<BasketViewModel> GetOrCreateBasketForUser(string userName)
        {
            var basket = await _session.Query<Basket>()
                .Include(x=>x.Items.Select(i=>i.CatalogItem))
                .FirstAsync(x => x.BuyerId == userName);

            if (basket == null)
            {
                basket = new Basket(userName);
                await _session.StoreAsync(basket, "baskets|");
                await _session.SaveChangesAsync();
            }

            return BasketToViewModel(basket);
        }

        private BasketViewModel BasketToViewModel(Basket basket)
        {
            var model = new BasketViewModel
            {
                BuyerId = basket.BuyerId,
                Id = basket.Id,
                Items = basket.Items.Select(i =>
                {
                    var itemModel = new BasketItemViewModel
                    {
                        Id = i.Id,
                        CatalogItemId = i.CatalogItemId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    };

                    var catalogItemTask = _session.LoadAsync<CatalogItem>("Catalog/" + i.CatalogItemId);
                    if (catalogItemTask.Status != TaskStatus.RanToCompletion)
                        throw new InvalidOperationException("Should never happen: " + catalogItemTask.Status, catalogItemTask.Exception);

                    var catalogItem = catalogItemTask.Result;
                    itemModel.OldUnitPrice = catalogItem.Price;
                    itemModel.ProductName = catalogItem.Name;
                    _uriComposer.ComposePicUri(catalogItem.PictureUri);

                    return itemModel;
                }).ToList()
            };
            return model;
        }
    }
}
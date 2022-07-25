using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace GeekBurger.Products.Controllers
{
    [Route("[controller]")]
    public class ProductsController : Controller
    {
        private IList<Product> Products;

        public ProductsController()
        {
            Products = GenarateMockListProduct();
        }

        [HttpGet("{storename}")]
        public IActionResult Get(string storeName)
        {
            try
            {
                var productsByStore = Products.Where(product => product.StoreName.Equals(storeName));

                if (productsByStore.Count() <= 0) return NotFound();

                return Ok(productsByStore);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }


        private List<Product> GenarateMockListProduct()
        {

            Item beef = new Item { ItemId = Guid.NewGuid(), Name = "beef" };
            Item pork = new Item { ItemId = Guid.NewGuid(), Name = "pork" };
            Item mustard = new Item { ItemId = Guid.NewGuid(), Name = "mustard" };
            Item ketchup = new Item { ItemId = Guid.NewGuid(), Name = "ketchup" };
            Item bread = new Item { ItemId = Guid.NewGuid(), Name = "bread" };
            Item wBread = new Item { ItemId = Guid.NewGuid(), Name = "whole bread" };

            return new List<Product>()
            {
                new Product {
                    Name = "Darth Bacon",
                    Image = "hamb1.png",
                    StoreName = "Paulista",
                    Items = new List<Item> {beef, ketchup, bread }
                },
                new Product {
                    Name = "Cap. Spork",
                    Image = "hamb2.png",
                    StoreName = "Paulista",
                    Items = new List<Item> { pork, mustard, wBread }
                },
                new Product {
                    Name = "Beef Turner",
                    Image = "hamb3.png",
                    StoreName = "Morumbi",
                    Items = new List<Item> {beef, mustard, bread }
                }
            };
        }
    }
}

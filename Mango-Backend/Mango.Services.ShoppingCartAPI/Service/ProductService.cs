using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Newtonsoft.Json;
using ShoppingCartAPI.Models.Dto;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class ProductService : IProductService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProductService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IEnumerable<ProductDto>> GetProductsAsync()
        {
            var client = _httpClientFactory.CreateClient("Product");

            var response = await client.GetAsync("/api/product");

            if (!response.IsSuccessStatusCode)
                return new List<ProductDto>();

            var apiContent = await response.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<ResponseDto<IEnumerable<ProductDto>>>(apiContent);

            return res?.Result ?? new List<ProductDto>();
        }
    }
}

using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Service.IService;
using Newtonsoft.Json;
using ShoppingCartAPI.Models.Dto;
using System.Net.Http;

namespace Mango.Services.ShoppingCartAPI.Service
{
    public class CouponService : ICouponService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CouponService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<CouponDto> GetCouponAsync(string couponCode)
        {
            var client = _httpClientFactory.CreateClient("Coupon");
            var response = await client.GetAsync($"/api/coupon/GetByCode/{couponCode}");
            if (!response.IsSuccessStatusCode)
                return null;

            var apiContent = await response.Content.ReadAsStringAsync();

            var res = JsonConvert.DeserializeObject<ResponseDto<CouponDto>>(apiContent);

            if (res != null && res.IsSuccess && res.Result != null)
            {
                return res.Result;
            }

            return null;
        }
    }
}

using Mango.Services.ProductAPI.Constants;
using Mango.Services.ProductAPI.Models.Dto;
using Mango.Services.ProductAPI.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(new ResponseDto<IEnumerable<ProductDto>>
            {
                Result = await _productService.GetAllProductsAsync()
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound(new ResponseDto<ProductDto> { IsSuccess = false, Message = "Product not found" });
            return Ok(new ResponseDto<ProductDto> { Result = product });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost]
        public async Task<IActionResult> Post(ProductDto productDto)
        {
            return Ok(new ResponseDto<ProductDto>
            {
                Result = await _productService.CreateProductAsync(productDto, Request)
            });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPut]
        public async Task<IActionResult> Update(ProductDto productDto)
        {
            return Ok(new ResponseDto<ProductDto>
            {
                Result = await _productService.UpdateProductAsync(productDto, Request)
            });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return Ok(new ResponseDto<string> { Result = "Deleted successfully" });
        }
    }
}
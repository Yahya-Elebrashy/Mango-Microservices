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
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("GET /api/product — Retrieving all products");

            var products = await _productService.GetAllProductsAsync();

            _logger.LogInformation("GET /api/product — Returned {Count} products", products.Count());
            return Ok(new ResponseDto<IEnumerable<ProductDto>> { Result = products });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            _logger.LogInformation("GET /api/product/{ProductId} — Retrieving product", id);

            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
            {
                _logger.LogWarning("GET /api/product/{ProductId} — Product not found", id);
                return NotFound(new ResponseDto<ProductDto> { IsSuccess = false, Message = "Product not found" });
            }

            _logger.LogInformation("GET /api/product/{ProductId} — Product found and returned", id);
            return Ok(new ResponseDto<ProductDto> { Result = product });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost]
        public async Task<IActionResult> Post(ProductDto productDto)
        {
            _logger.LogInformation("POST /api/product — Creating product. Name={Name}, Price={Price}, Category={Category}",
                productDto.Name, productDto.Price, productDto.CategoryName);

            var created = await _productService.CreateProductAsync(productDto, Request);

            _logger.LogInformation("POST /api/product — Product created successfully with ID {ProductId}", created.ProductId);
            return Ok(new ResponseDto<ProductDto> { Result = created });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPut]
        public async Task<IActionResult> Update(ProductDto productDto)
        {
            _logger.LogInformation("PUT /api/product — Updating product with ID {ProductId}", productDto.ProductId);

            var updated = await _productService.UpdateProductAsync(productDto, Request);

            _logger.LogInformation("PUT /api/product — Product {ProductId} updated successfully", updated.ProductId);
            return Ok(new ResponseDto<ProductDto> { Result = updated });
        }

        [Authorize(Roles = SD.RoleAdmin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("DELETE /api/product/{ProductId} — Deleting product", id);

            await _productService.DeleteProductAsync(id);

            _logger.LogInformation("DELETE /api/product/{ProductId} — Product deleted successfully", id);
            return Ok(new ResponseDto<string> { Result = "Deleted successfully" });
        }
    }
}
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Services.IServices;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?>             GetProductByIdAsync(int id);
    Task<ProductDto>              CreateProductAsync(ProductDto productDto, HttpRequest request);
    Task<ProductDto>              UpdateProductAsync(ProductDto productDto, HttpRequest request);
    Task                          DeleteProductAsync(int id);
}

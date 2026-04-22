using AutoMapper;
using Azure;
using Mango.Services.ProductAPI.Constants;
using Mango.Services.ProductAPI.Data;
using Mango.Services.ProductAPI.Models.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.Models;
using ProductAPI.Models.Dto;

namespace Mango.Services.ProductAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        public ProductController(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        [HttpGet]
        public ResponseDto<IEnumerable<ProductDto>> Get()
        {
            var response = new ResponseDto<IEnumerable<ProductDto>>();
            try
            {
                var products = _db.Products.ToList();
                response.Result = _mapper.Map<IEnumerable<ProductDto>>(products);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
       
        [HttpGet("{id:int}")]
        public ResponseDto<ProductDto> Get(int id)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                var product = _db.Products.FirstOrDefault(x => x.ProductId == id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }

                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
        
        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPost]
        public ResponseDto<ProductDto> Post(ProductDto productDto)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                Product product = _mapper.Map<Product>(productDto);
                _db.Products.Add(product);
                _db.SaveChanges();
                if (productDto.Image != null)
                {

                    string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;

                    //I have added the if condition to remove the any image with same name if that exist in the folder by any change
                    var directoryLocation = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    FileInfo file = new FileInfo(directoryLocation);
                    if (file.Exists)
                    {
                        file.Delete();
                    }

                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        productDto.Image.CopyTo(fileStream);
                    }
                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    product.ImageLocalPath = filePath;
                }
                else
                {
                    product.ImageUrl = "https://placehold.co/600x400";
                }
                _db.Products.Update(product);
                _db.SaveChanges();
                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
        
        [Authorize(Roles = SD.RoleAdmin)]
        [HttpPut]
        public ResponseDto<ProductDto> Update(ProductDto productDto)
        {
            var response = new ResponseDto<ProductDto>();
            try
            {
                var product = _db.Products.FirstOrDefault(x => x.ProductId == productDto.ProductId);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }
                _mapper.Map(productDto, product);
                if (productDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(product.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }

                    string fileName = product.ProductId + Path.GetExtension(productDto.Image.FileName);
                    string filePath = @"wwwroot\ProductImages\" + fileName;
                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        productDto.Image.CopyTo(fileStream);
                    }
                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    product.ImageUrl = baseUrl + "/ProductImages/" + fileName;
                    product.ImageLocalPath = filePath;
                }
                _db.Products.Update(product);
                _db.SaveChanges();
                response.Result = _mapper.Map<ProductDto>(product);
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.RoleAdmin)]
        public ResponseDto<string> Delete(int id)
        {
            var response = new ResponseDto<string>();
            try
            {
                var product = _db.Products.FirstOrDefault(x => x.ProductId == id);

                if (product == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Product not found";
                    return response;
                }
                if (!string.IsNullOrEmpty(product.ImageLocalPath))
                {
                    var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), product.ImageLocalPath);
                    FileInfo file = new FileInfo(oldFilePathDirectory);
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }
                _db.Remove(product);
                _db.SaveChanges();
                response.Result = "Deleted successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
            }
            return response;
        }
    }
}

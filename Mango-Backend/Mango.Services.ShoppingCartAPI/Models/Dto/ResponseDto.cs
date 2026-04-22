namespace Mango.Services.ShoppingCartAPI.Models.Dto
{
    public class ResponseDto<T>
    {
        public T? Result { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}

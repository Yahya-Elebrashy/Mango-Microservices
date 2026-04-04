using Mango.Services.EmailAPI.Data;
using Mango.Services.EmailAPI.Models;
using Mango.Services.EmailAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Mango.Services.EmailAPI.Services
{
    public class EmailService : IEmailService
    {
        private DbContextOptions<AppDbContext> _dbContextOptions;
        public EmailService(DbContextOptions<AppDbContext> dbContextOptions) 
        {
            _dbContextOptions = dbContextOptions;
        }
        public async Task EmailCartAndLog(CartDto cartDto)
        {
            StringBuilder message = new StringBuilder();
            message.Append("<br/>Cart Email Requested ");
            message.AppendLine("<br/> total " + cartDto.CartHeader.CartTotal);
            message.Append("<br/>");
            message.Append("<ul>");
            foreach(var item in cartDto.CartDetails)
            {
                message.Append("<li>");

                message.Append(item.ProductDto.Name + " x " + item.Count);
                message.Append("/<li>");
            }
            message.Append("</ul>");
            await LogAndEmail(message.ToString(), cartDto.CartHeader.Email);
        }
        public async Task LogOrderPlaced(RewardsDto rewardsDto)
        {
            string message = "New Order Placed. <br/> Order ID : " + rewardsDto.OrderId;
            await LogAndEmail(message, "dotnetmastery@gmail.com");
        }
        private async Task<bool> LogAndEmail(string message , string email)
        {
            try
            {
                EmailLogger emailLogger = new()
                {
                    Email = email,
                    Message = message,
                    EmailSent = DateTime.Now
                };
                await using var _db = new AppDbContext(_dbContextOptions);
                _db.EmailLoggers.Add(emailLogger);  
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}

using Mango.Services.AuthAPI.Data;
using Mango.Services.AuthAPI.Models;
using Mango.Services.AuthAPI.Models.Dto;
using Mango.Services.AuthAPI.Service.IService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.AuthAPI.Service
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(AppDbContext db, UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator, RoleManager<IdentityRole> roleManager
            )
        {
            _db = db;
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _roleManager = roleManager;
        }

        public async Task<bool> AssignRole(string email, string roleName)
        {
            var user = _db.applicationUsers.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user is not null)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                await _userManager.AddToRoleAsync(user, roleName);
                return true;
            }
            return false;
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto dto)
        {
            var user = await _db.applicationUsers
            .FirstOrDefaultAsync(u => u.UserName!.ToLower() == dto.UserName.ToLower());

            // Guard clause — user not found
            if (user is null)
                return new LoginResponseDto { User = null, Token = string.Empty };

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            // Guard clause — wrong password
            if (!isPasswordValid)
                return new LoginResponseDto { User = null, Token = string.Empty };

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            return new LoginResponseDto
            {
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Name = user.Name,
                    PhoneNumber = user.PhoneNumber!
                },
                Token = token
            };
        }

        public async Task<string> Register(RegisterationRequestDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return result.Errors.FirstOrDefault()?.Description ?? "Registration failed";

            return string.Empty;
        }
    }
}

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
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            AppDbContext db,
            UserManager<ApplicationUser> userManager,
            IJwtTokenGenerator jwtTokenGenerator,
            RoleManager<IdentityRole> roleManager,
            ILogger<AuthService> logger)
        {
            _db = db;
            _userManager = userManager;
            _jwtTokenGenerator = jwtTokenGenerator;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<bool> AssignRole(string email, string roleName)
        {
            _logger.LogInformation("Attempting to assign role {Role} to user {Email}", roleName, email);

            var user = _db.applicationUsers.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());

            if (user is null)
            {
                _logger.LogWarning("AssignRole failed — no user found with email {Email}", email);
                return false;
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                _logger.LogInformation("Role {Role} does not exist — creating it", roleName);
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("Role {Role} created successfully", roleName);
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign role {Role} to user {Email}. Errors: {Errors}", roleName, email, errors);
                return false;
            }

            _logger.LogInformation("Role {Role} assigned successfully to user {Email} (ID={UserId})", roleName, email, user.Id);
            return true;
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto dto)
        {
            _logger.LogInformation("Login attempt for username {Username}", dto.UserName);

            var user = await _db.applicationUsers
                .FirstOrDefaultAsync(u => u.UserName!.ToLower() == dto.UserName.ToLower());

            if (user is null)
            {
                _logger.LogWarning("Login failed — no user found with username {Username}", dto.UserName);
                return new LoginResponseDto { User = null, Token = string.Empty };
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordValid)
            {
                _logger.LogWarning("Login failed — invalid password for username {Username} (ID={UserId})", dto.UserName, user.Id);
                return new LoginResponseDto { User = null, Token = string.Empty };
            }

            var roles = await _userManager.GetRolesAsync(user);
            _logger.LogInformation("User {Username} (ID={UserId}) authenticated. Roles=[{Roles}]", user.UserName, user.Id, string.Join(", ", roles));

            var token = _jwtTokenGenerator.GenerateToken(user, roles);
            _logger.LogInformation("JWT token generated successfully for user {Username} (ID={UserId})", user.UserName, user.Id);

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
            _logger.LogInformation("Registration attempt for email {Email}", dto.Email);

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                Name = dto.Name,
                PhoneNumber = dto.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var error = result.Errors.FirstOrDefault()?.Description ?? "Registration failed";
                _logger.LogWarning("Registration failed for email {Email}. Reason: {Error}", dto.Email, error);
                return error;
            }

            _logger.LogInformation("User registered successfully. Email={Email}, ID={UserId}", dto.Email, user.Id);
            return string.Empty;
        }
    }
}
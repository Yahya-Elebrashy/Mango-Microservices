using Mango.Services.AuthAPI.Data;
using Mango.Services.AuthAPI.Models;
using Mango.Services.AuthAPI.Models.Dto;
using Mango.Services.AuthAPI.Service.IService;
using Microsoft.AspNetCore.Identity;

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

        public async Task<bool> AssignRole(string email, string roleNname)
        {
            var user = _db.applicationUsers.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user is not null)
            {
                if (!_roleManager.RoleExistsAsync(roleNname).GetAwaiter().GetResult())
                {
                    // Create role 
                    _roleManager.CreateAsync(new IdentityRole(roleNname)).GetAwaiter().GetResult();
                }
                await _userManager.AddToRoleAsync(user, roleNname);
                return true;
            }
            return false;
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            var user = _db.applicationUsers.FirstOrDefault(u => u.UserName.ToLower() == loginRequestDto.UserName.ToLower());
            var isValid = await _userManager.CheckPasswordAsync(user, loginRequestDto.Password);
            if (isValid ==  false || user == null)
            {
                return new LoginResponseDto
                {
                    User = null,
                    Token = ""
                };
            }
            // if user was found 
            UserDto userDto = new UserDto
            {
               Id = user.Id,
               Email = user.Email,
               Name = user.Name,
               PhoneNumber = user.PhoneNumber
            };
            // Generate Token
            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenGenerator.GenerateToken(user, roles);
            return new LoginResponseDto
            {
                User = userDto,
                Token = token
            };
        }

        public async Task<string> Register(RegisterationRequestDto registerationRequestDto)
        {
            ApplicationUser user = new()
            {
                UserName = registerationRequestDto.Email,
                Email = registerationRequestDto.Email,
                Name = registerationRequestDto.Name,
                PhoneNumber = registerationRequestDto.PhoneNumber
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registerationRequestDto.Password);
                if (result.Succeeded)
                {
                    var userToReturn = _db.Users.First(u => u.UserName == registerationRequestDto.Email);
                    UserDto userDto= new UserDto
                    {
                        Email = userToReturn.Email,
                        Name = userToReturn.Name,
                        PhoneNumber = userToReturn.PhoneNumber,
                        Id = userToReturn.Id
                    };
                    return "";
                }
                else
                {
                    return result.Errors.FirstOrDefault().Description;
                }
            }
            catch(Exception ex) {
            }
            return "Error Encountered";
        }
    }
}

﻿using Azure.Core;
using Connectify.BusinessObjects.Authen;
using Connectify.BussinessObjects.Authen;
using Connectify.Server.DataAccess;
using Connectify.Server.DTOs;
using Connectify.Server.Services.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace Connectify.Server.Services.Implement { 
    public class AccountService : IAccountService
    {
        private readonly UserManager<User> userManager;
        private readonly SignInManager<User> signInManager;
        private readonly IConfiguration configuration;
        private readonly AppDbContext dbContext;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountService(UserManager<User> userManager,
            SignInManager<User> signInManager, AppDbContext dbContext,
            IConfiguration configuration, RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.configuration = configuration;
            this.dbContext = dbContext;
            this.roleManager = roleManager;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
        private string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]));
            var token = new JwtSecurityToken(
                issuer: configuration["JWT:ValidIssuer"],
                audience: configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddSeconds(1000000000),
                claims: claims,
                signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha512Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private async Task SaveRefreshTokenAsync(string userId, string refreshToken)
        {
            var token = new RefreshToken
            {
                Token = refreshToken,
                ExpiryDate = DateTime.Now.AddMinutes(5),
                UserId = userId,
                Id = new Guid()
            };
            await dbContext.RefreshTokens.AddAsync(token);
            await dbContext.SaveChangesAsync();
        }
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"])),
                ValidateLifetime = false // we want to get claims from expired token
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid tokasdfen");
            }

            return principal;
        }
        private async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
        {
            var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);
            if (token == null || token.ExpiryDate <= DateTime.Now)
            {
                return false;
            }

            dbContext.RefreshTokens.Remove(token);
            await dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<IdentityResult> SignUpAsync(SignUpDTO dto)
        {

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                UserName = dto.Email,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender
            };
            var result = await userManager.CreateAsync(user, dto.Password);
            if (result.Succeeded)
            {
                if (!await roleManager.RoleExistsAsync(AppRole.NormalUser))
                {
                    await roleManager.CreateAsync(new IdentityRole(AppRole.NormalUser));
                }
                await userManager.AddToRoleAsync(user, AppRole.NormalUser);
            }
            return result;
        }
        public async Task<TokenDTO?> SignInAsync(SignInDTO dto)
        {
            var result = await signInManager.PasswordSignInAsync(dto.Email, dto.Password, false, false);
            if (!result.Succeeded)
            {
                return null;
            }

            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return null;
            }
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, dto.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }
            var refreshToken = GenerateRefreshToken();
            await SaveRefreshTokenAsync(user.Id, refreshToken);
            var accessToken = GenerateAccessToken(authClaims);
            return new TokenDTO { AccessToken = accessToken, RefreshToken = refreshToken };
        }
        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var token = await dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token == null)
            {
                return false;
            }

            dbContext.RefreshTokens.Remove(token);
            await dbContext.SaveChangesAsync();
            return true;
        }
        public async Task<TokenDTO?> RefreshTokenAsync(TokenDTO tokenDto)
        {
            var principal = GetPrincipalFromExpiredToken(tokenDto.AccessToken);
            var email = principal.FindFirstValue(ClaimTypes.Email);
            var user = await userManager.FindByEmailAsync(email);

            if (user == null || !await ValidateRefreshTokenAsync(user.Id, tokenDto.RefreshToken))
            {
                return null;
            }

            var accessToken = GenerateAccessToken(principal.Claims);
            var refreshToken = GenerateRefreshToken();

            await SaveRefreshTokenAsync(user.Id, refreshToken);

            return new TokenDTO { AccessToken = accessToken, RefreshToken = refreshToken };
        }
    }
}

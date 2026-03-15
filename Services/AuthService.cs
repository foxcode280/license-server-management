using BCrypt.Net;
using LicenseManager.API.DTOs;
using LicenseManager.API.Helpers;
using LicenseManager.API.Models;
using LicenseManager.API.Repositories;
using Npgsql;

namespace LicenseManager.API.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepo;
        private readonly RefreshTokenRepository _refreshRepo;
        private readonly TokenService _tokenService;
        private readonly LoginHistoryRepository _loginRepo;
        private readonly JwtService _jwt;

        public AuthService(UserRepository userRepo, RefreshTokenRepository refreshRepo, TokenService tokenService, LoginHistoryRepository loginRepo, JwtService jwt)
        {
            _userRepo = userRepo;
            _refreshRepo = refreshRepo;
            _loginRepo = loginRepo;
            _tokenService = tokenService;
            _jwt = jwt;
        }

        public async Task<LoginResponse?> Login(LoginRequest request, string ip, string userAgent)
        {
            var user = await _userRepo.GetUserByEmail(request.Email);

            if (user == null)
            {
                await _loginRepo.SaveLoginHistory(new LoginHistory
                {
                    UserId = 0,
                    Email = request.Email,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    LoginStatus = "FAILED",
                    FailureReason = "User not found"
                });
                return null;
            }


            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                await _loginRepo.SaveLoginHistory(new LoginHistory
                {
                    UserId = user.Id,
                    Email = request.Email,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    LoginStatus = "FAILED",
                    FailureReason = "Invalid password"
                });
                return null;
            }

            // Generate Access Token
            var accessToken = _jwt.GenerateToken(user);

            // Generate Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Save refresh token in DB
            await _refreshRepo.SaveRefreshToken(
                user.Id,
                refreshToken,
                DateTime.UtcNow.AddDays(7)
            );
            await _loginRepo.SaveLoginHistory(new LoginHistory
            {
                UserId = user.Id,
                Email = user.Email,
                IpAddress = ip,
                UserAgent = userAgent,
                LoginStatus = "SUCCESS",
                FailureReason = ""
            });

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Email = user.Email,
                Role = user.Role
            };
        }
        public async Task<RefreshToken?> ValidateRefreshToken(string refreshToken)
        {
            return await _refreshRepo.GetRefreshToken(refreshToken);
        }

        public async Task<User?> GetUserById(long id)
        {
            return await _userRepo.GetUserById(id);
        }
        public async Task RevokeRefreshToken(string token)
        {
            await _refreshRepo.RevokeRefreshToken(token);
        }
        public async Task<LoginResponse> RotateRefreshToken(User user, string oldToken)
        {
            // Generate new tokens
            var newAccessToken = _jwt.GenerateToken(user);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Rotate refresh token in DB
            await _refreshRepo.RotateRefreshToken(
                oldToken,
                newRefreshToken,
                DateTime.UtcNow.AddDays(7)
            );

            return new LoginResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
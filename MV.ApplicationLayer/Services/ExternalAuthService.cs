using Microsoft.Extensions.Configuration;
using MV.ApplicationLayer.Interfaces;
using MV.DomainLayer.DTOs.Login.Response;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.Interfaces;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MV.ApplicationLayer.Services
{
    public class ExternalAuthService : IExternalAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;

        public ExternalAuthService(
            IHttpClientFactory httpClientFactory,
            IUserRepository userRepository,
            IJwtService jwtService,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _userRepository = userRepository;
            _jwtService = jwtService;
            _config = config;
        }

        public async Task<LoginResponseDto> GoogleLoginAsync(string code, string? redirectUri)
        {
            var clientId = _config["OAuth:Google:ClientId"];
            var clientSecret = _config["OAuth:Google:ClientSecret"];

            var client = _httpClientFactory.CreateClient();

            // Google's token endpoint requires application/x-www-form-urlencoded (not JSON)
            var formFields = new List<KeyValuePair<string, string>>
            {
                new("client_id", clientId ?? ""),
                new("client_secret", clientSecret ?? ""),
                new("code", code),
                new("grant_type", "authorization_code"),
                new("redirect_uri", redirectUri ?? "")
            };
            var formContent = new FormUrlEncodedContent(formFields);

            var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token", formContent);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                var error = await tokenResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to exchange Google code: {error}");
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GoogleTokenResponse>();
            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.IdToken))
            {
                throw new Exception("Invalid Google token response");
            }

            // Decode IdToken (which is a JWT) to get user info. 
            // In a production environment, you should properly validate the signature of this IdToken.
            // For simplicity in this flow, we will decode the payload directly.
            var parts = tokenResult.IdToken.Split('.');
            if (parts.Length != 3) throw new Exception("Invalid IdToken format");

            var payload = parts[1];
            // Fix base64url encoding
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(jsonBytes);

            if (userInfo == null || string.IsNullOrEmpty(userInfo.Email))
            {
                throw new Exception("Could not extract email from Google IdToken");
            }

            return await LinkOrCreateUserAsync("Google", userInfo.Sub, userInfo.Email, userInfo.Name, userInfo.Picture);
        }

        public async Task<LoginResponseDto> GitHubLoginAsync(string code, string? redirectUri)
        {
            var clientId = _config["OAuth:GitHub:ClientId"];
            var clientSecret = _config["OAuth:GitHub:ClientSecret"];

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var tokenRequest = new
            {
                client_id = clientId,
                client_secret = clientSecret,
                code = code,
                redirect_uri = redirectUri
            };

            var tokenResponse = await client.PostAsJsonAsync("https://github.com/login/oauth/access_token", tokenRequest);
            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new Exception("Failed to exchange GitHub code");
            }

            var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<GitHubTokenResponse>();
            if (tokenResult == null || string.IsNullOrEmpty(tokenResult.AccessToken))
            {
                throw new Exception("Invalid GitHub token response");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MV.API", "1.0"));

            var userResponse = await client.GetAsync("https://api.github.com/user");
            if (!userResponse.IsSuccessStatusCode) throw new Exception("Failed to get GitHub user profile");

            var userInfo = await userResponse.Content.ReadFromJsonAsync<GitHubUserInfo>();
            
            // Get email since it might not be in the public profile
            string email = userInfo.Email;
            if (string.IsNullOrEmpty(email))
            {
                var emailsResponse = await client.GetAsync("https://api.github.com/user/emails");
                if (emailsResponse.IsSuccessStatusCode)
                {
                    var emails = await emailsResponse.Content.ReadFromJsonAsync<GitHubEmail[]>();
                    if (emails != null)
                    {
                        var primaryEmail = Array.Find(emails, e => e.Primary);
                        if (primaryEmail != null) email = primaryEmail.Email;
                    }
                }
            }

            if (string.IsNullOrEmpty(email))
            {
                throw new Exception("Could not retrieve email from GitHub");
            }

            return await LinkOrCreateUserAsync("GitHub", userInfo.Id.ToString(), email, userInfo.Name ?? userInfo.Login, userInfo.AvatarUrl);
        }

        private async Task<LoginResponseDto> LinkOrCreateUserAsync(string provider, string externalId, string email, string name, string avatarUrl)
        {
            var user = await _userRepository.GetByExternalLoginAsync(provider, externalId);

            if (user == null)
            {
                user = await _userRepository.GetByEmailAsync(email);
                if (user != null)
                {
                    // Link existing account
                    user.ExternalProvider = provider;
                    user.ExternalId = externalId;
                    if (string.IsNullOrEmpty(user.AvatarUrl) && !string.IsNullOrEmpty(avatarUrl))
                    {
                        user.AvatarUrl = avatarUrl;
                    }
                    await _userRepository.UpdateAsync(user);
                }
                else
                {
                    // Create new account
                    var defaultRoleName = "Customer";
                    var role = await _userRepository.GetRoleByNameAsync(defaultRoleName);
                    
                    user = new User
                    {
                        Username = name ?? email.Split('@')[0],
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        RoleId = role?.RoleId ?? 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        ExternalProvider = provider,
                        ExternalId = externalId,
                        AvatarUrl = avatarUrl
                    };
                    await _userRepository.AddAsync(user);
                    
                    if (role != null)
                    {
                        user.Role = role;
                    }
                }
            }
            
            if (user == null || user.Role == null) throw new Exception("Failed to load user information.");

            return new LoginResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.RoleName,
                AccessToken = _jwtService.GenerateToken(user)
            };
        }

        private class GoogleTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
            [JsonPropertyName("id_token")]
            public string IdToken { get; set; }
        }

        private class GoogleUserInfo
        {
            [JsonPropertyName("sub")]
            public string Sub { get; set; }
            [JsonPropertyName("email")]
            public string Email { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("picture")]
            public string Picture { get; set; }
        }

        private class GitHubTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }
        }

        private class GitHubUserInfo
        {
            [JsonPropertyName("id")]
            public long Id { get; set; }
            [JsonPropertyName("login")]
            public string Login { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("email")]
            public string Email { get; set; }
            [JsonPropertyName("avatar_url")]
            public string AvatarUrl { get; set; }
        }

        private class GitHubEmail
        {
            [JsonPropertyName("email")]
            public string Email { get; set; }
            [JsonPropertyName("primary")]
            public bool Primary { get; set; }
            [JsonPropertyName("verified")]
            public bool Verified { get; set; }
        }
    }
}

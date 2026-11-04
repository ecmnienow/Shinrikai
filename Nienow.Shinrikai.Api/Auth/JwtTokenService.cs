using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Auth
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "Shinrikai";
        public string Audience { get; set; } = "Shinrikai";
        /// <summary>Chave HMAC (mín. 32 bytes). Em produção, via variável de ambiente Jwt__Key ou user-secrets.</summary>
        public string Key { get; set; } = string.Empty;
        public int ExpirationMinutes { get; set; } = 8 * 60;

        public SymmetricSecurityKey SigningKey()
        {
            if (Encoding.UTF8.GetByteCount(Key) < 32)
                throw new InvalidOperationException("Jwt:Key ausente ou com menos de 32 bytes.");
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        }
    }

    public static class ShinrikaiClaims
    {
        public const string Jlpt = "jlpt";
        /// <summary>Substitui o Session.SessionID usado em INTERACTIONS.ITR_SESSIONID.</summary>
        public const string SessionId = "shinrikai_session";
    }

    public sealed record AuthenticatedUser(string Token, DateTime ExpiresAt, UserInfo User);

    public sealed record UserInfo(int Id, string? Login, string? Name, EJLPT Jlpt, string Role)
    {
        public static UserInfo From(TUser user) => new(user.ID, user.Login, user.Name, user.JLPT, user.Role);
    }

    public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock)
    {
        public AuthenticatedUser Issue(TUser user)
        {
            var o = options.Value;
            var now = clock.GetUtcNow().UtcDateTime;
            var expires = now.AddMinutes(o.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: o.Issuer,
                audience: o.Audience,
                claims:
                [
                    new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                    new(ClaimTypes.Name, user.Login ?? string.Empty),
                    new(ClaimTypes.GivenName, user.Name ?? string.Empty),
                    new(ClaimTypes.Role, user.Role),
                    new(ShinrikaiClaims.Jlpt, user.JLPT.ToString()),
                    new(ShinrikaiClaims.SessionId, Guid.NewGuid().ToString("N")),
                ],
                notBefore: now,
                expires: expires,
                signingCredentials: new SigningCredentials(o.SigningKey(), SecurityAlgorithms.HmacSha256));

            return new AuthenticatedUser(new JwtSecurityTokenHandler().WriteToken(token), expires, UserInfo.From(user));
        }
    }

    public static class ClaimsPrincipalExtensions
    {
        public static int UserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public static EJLPT Jlpt(this ClaimsPrincipal user) =>
            Enum.TryParse<EJLPT>(user.FindFirstValue(ShinrikaiClaims.Jlpt), out var lvl) ? lvl : EJLPT.None;

        public static string? SessionId(this ClaimsPrincipal user) => user.FindFirstValue(ShinrikaiClaims.SessionId);
    }
}

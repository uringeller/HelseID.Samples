﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using IdentityModel.OidcClient;
using Microsoft.IdentityModel.Tokens;

namespace HelseID.Test.WPF.Common
{
    public class JwtGenerator
    {
        public static List<string> ValidAudiences = new List<string> { "https://localhost:44366/connect/token", "https://helseid-sts.utvikling.nhn.no", "https://helseid-sts.test.nhn.no", "https://helseid-sts.utvikling.nhn.no"};
        private const double DefaultExpiryInHours = 10;

        /// <summary>
        /// Generates a new JWT, uses Cng (RSA) to get PK SigningCredentials
        /// </summary>
        /// <param name="clientId">The OAuth/OIDC client ID</param>
        /// <param name="audience">The Authorization Server (STS)</param>
        /// <param name="expiryDate">If value is null, the default expiry date is used (10 hrs)</param>
        /// <param name="signingCredentials">SigningCredentials for JWS, if this is null we get the credentials for existing CngKey or create a new PK pair.</param>
        /// <returns></returns>
        public static string Generate(string clientId, string audience, DateTime? expiryDate, SigningCredentials signingCredentials = null)
        {
            if (signingCredentials == null)
                signingCredentials = GetSigningCredentials();
            
            var jwt = CreateJwt(clientId, audience + "", expiryDate, signingCredentials);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(jwt);
            return token;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clientOptions"></param>
        public static string Generate(OidcClientOptions clientOptions)
        {
            return Generate(clientOptions.ClientId, clientOptions.Authority, null);
        }

        private static JwtSecurityToken CreateJwt(string clientId, string audience, DateTime? expiryDate, SigningCredentials signingCredentials)
        {
            var exp = new DateTimeOffset(expiryDate ?? DateTime.Now.AddHours(DefaultExpiryInHours));

            var claims = new List<Claim>
            {
                new Claim("sub", clientId),
                new Claim("iat", exp.ToUnixTimeSeconds().ToString()),
                new Claim("jti", Guid.NewGuid().ToString("N"))
            };

            var token = new JwtSecurityToken(clientId, audience + "connect/token", claims, DateTime.Now, DateTime.Now.AddHours(10), signingCredentials);
            
            return token;
        }

        private static SigningCredentials GetSigningCredentials()
        {            
            var rsa = RSAKeyGenerator.GetRsaParameters();            
            var securityKey = new RsaSecurityKey(rsa);

            var signingCredentials = new SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.RsaSha512);//RSAKeyGenerator.JwsAlgorithmName);
            var kid = signingCredentials.Kid;
            return signingCredentials;
        }

        public static SecurityToken ValidateToken(string token, string validIssuer)
        {            
            var publicKey = RSAKeyGenerator.GetPublicKeyAsXml();

            var test = RSA.Create();
            test.FromXmlString(publicKey);

            var securityKey = new RsaSecurityKey(test.ExportParameters(false));

            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                IssuerSigningKey = securityKey,
                ValidAudiences = ValidAudiences,
                ValidIssuer = validIssuer
            };

            var claimsPrincipal = handler.ValidateToken(token, validationParams, out SecurityToken validatedToken);

            return validatedToken;

        }


    }
}

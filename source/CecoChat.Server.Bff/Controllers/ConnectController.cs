using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CecoChat.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CecoChat.Server.Bff.Controllers
{
    [ApiController]
    [Route("api/connect")]
    public class ConnectController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly JwtOptions _jwtOptions;
        private readonly IClock _clock;

        private readonly SigningCredentials _signingCredentials;
        private readonly JwtSecurityTokenHandler _jwtTokenHandler;

        public ConnectController(
            ILogger<ConnectController> logger,
            IOptions<JwtOptions> jwtOptions,
            IClock clock)
        {
            _logger = logger;
            _jwtOptions = jwtOptions.Value;
            _clock = clock;

            byte[] secret = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
            _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature);
            _jwtTokenHandler = new();
            _jwtTokenHandler.OutboundClaimTypeMap.Clear();
        }

        [HttpPost(Name = "Connect")]
        [ProducesResponseType(typeof(ConnectResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<ConnectResponse> Connect([FromBody][BindRequired] ConnectRequest request)
        {
            if (!_userIDMap.TryGetValue(request.Username, out long userID))
            {
                return Unauthorized();
            }
            Activity.Current?.AddTag("user.id", userID);

            (Guid clientID, string accessToken) = CreateSession(userID); 
            _logger.LogInformation("User {0} authenticated and assigned user ID {1} and client ID {2}.", request.Username, userID, clientID);

            ConnectResponse response = new()
            {
                ClientID = clientID,
                AccessToken = accessToken
            };
            return Ok(response);
        }

        private readonly Dictionary<string, long> _userIDMap = new()
        {
            {"bob", 1},
            {"alice", 2},
            {"peter", 1200}
        };

        private (Guid, string) CreateSession(long userID)
        {
            Guid clientID = Guid.NewGuid();
            Claim[] claims =
            {
                new(JwtRegisteredClaimNames.Sub, userID.ToString(), ClaimValueTypes.Integer64),
                new(ClaimTypes.Actor, clientID.ToString()),
                new(ClaimTypes.Role, "user")
            };

            DateTime expiration = _clock.GetNowUtc().Add(_jwtOptions.AccessTokenExpiration);
            JwtSecurityToken jwtToken = new(_jwtOptions.Issuer, _jwtOptions.Audience, claims, null, expiration, _signingCredentials);
            string accessToken = _jwtTokenHandler.WriteToken(jwtToken);

            return (clientID, accessToken);
        }
    }
}
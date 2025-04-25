// using Microsoft.Azure.Functions.Worker;
// using Microsoft.Extensions.Logging;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using System;
// using System.IO;
// using System.Threading.Tasks;
// // using Microsoft.AspNetCore.Mvc;
// // using Microsoft.Azure.WebJobs;
// using Microsoft.Azure.WebJobs.Extensions.Http;
// // using Microsoft.AspNetCore.Http;
// // using Microsoft.Extensions.Logging;
// using Newtonsoft.Json;
// using System.IdentityModel.Tokens.Jwt;
// using System.Linq;
// using System.Net;

// namespace WebhookFunctionProj
// {
//     public static class WebhookFunctionProj
// {
//     private static ILogger _logger;
//     private static HttpRequest _request;

//     /// <summary>
//     /// Entry point for the Azure Function. Receives a JSON payload and stores it in Cosmos DB.
//     /// </summary>
//     /// <param name="req">The HTTP request</param>
//     /// <param name="log">The ILogger for logging information</param>
//     /// <returns>An ActionResult Task</returns>
//     [Function("WebhookFunctionProj")]
//     public static async Task<IActionResult> Run(
//         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
//         ILogger log)
//     {
//         _request = req;
//         _logger = log;
            
//         _logger.LogInformation("Webhook invoked.");

//         // check the JWT and its claims to ensure the request is valid
//         if (!JwtIsValid())
//         {
//             _logger.LogCritical("JWT security checks failed!");

//             return new BadRequestResult();
//         }

//         #region

//         // read the input POST body
//         var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
//         dynamic body = JsonConvert.DeserializeObject(requestBody);

//         // create an item to store in Cosmos DB
//         var item = new
//         {
//             id = Guid.NewGuid().ToString(), 
//             payload = body
//         };
        
//         #endregion

//         return new OkResult();
//     }

//     /// <summary>
//     /// Checks that the JWT is valid.
//     /// </summary>
//     /// <returns>A bool indicating if the Claims in the JWT are valid.</returns>
//     private static bool JwtIsValid()
//     {
//         // Retrieve and ensure the Authorization header
//         string authHeader = _request.Headers["Authorization"];

//         if (string.IsNullOrEmpty(authHeader))
//         {
//             _logger.LogCritical("Authorization header is missing.");
            
//             return false;
//         }
            
//         // Get the JWT token from the Authorization header
//         var jwt = authHeader.Split(' ')[1]; // get the JWT portion of the Authorization header
//         _logger.LogInformation($"JWT token: {jwt}");
            
//         var handler = new JwtSecurityTokenHandler();

//         // Ensure the JWT is readable
//         if (!handler.CanReadToken(jwt))
//         {
//             _logger.LogCritical("Can't read JWT");

//             return false;
//         }

//         // Read the JWT
//         if (handler.ReadToken(jwt) is not JwtSecurityToken jwtToken)
//         {
//             _logger.LogCritical("JWT is missing");

//             return false;
//         }

//         // Check the claims in the JWT and return the result
//         return JwtClaimsAreValid(jwtToken);
//     }

//     /// <summary>
//     /// Ensure the claims in the JWT are valid.
//     /// </summary>
//     /// <param name="jwtToken"></param>
//     /// <returns>A bool indicating whether the required claims are valid</returns>
//     private static bool JwtClaimsAreValid(JwtSecurityToken jwtToken)
//     {
//         //==============================================================================================
//         // Use raw reading of claims to avoid dependency on a specific library or .NET ClaimsPrincipal
//         //==============================================================================================

//         var isValid = true;

//         // aud
//         // Check the Entra ID application ID in the offer's technical configuration in Partner Center
//         var clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud")!.Value;
//         var expectedClientId = Environment.GetEnvironmentVariable("ClientId");
//         if (clientId != expectedClientId)
//         {
//             _logger.LogWarning("Client ID does not match.");
//             _logger.LogWarning($"Expected Client ID: {expectedClientId}");
//             _logger.LogWarning($"Found Client ID: {clientId}");

//             isValid = false;
//         }

//         // tid
//         // Check the Entra tenant ID in the offer's technical configuration in Partner Center
//         var tenantId = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid")!.Value;
//         var expectedTenantId = Environment.GetEnvironmentVariable("TenantId");
//         if (tenantId != expectedTenantId)
//         {
//             _logger.LogWarning("Tenant ID does not match.");
//             _logger.LogWarning($"Expected Tenant ID: {expectedTenantId}");
//             _logger.LogWarning($"Found Tenant ID: {tenantId}");
            
//             isValid = false;
//         }

//         // iss
//         // Check the Entra tenant ID in the offer's technical configuration in Partner Center
//         var expectedIssuer = $"https://sts.windows.net/{expectedTenantId}/";
//         var issuer = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")!.Value;
//         if (issuer != expectedIssuer)
//         {
//             _logger.LogWarning("Issuer does not match.");
//             _logger.LogWarning($"Expected Issuer: {expectedIssuer}");
//             _logger.LogWarning($"Found Issuer: {tenantId}");

//             isValid = false;
//         }

        
//         // appid or azp
//         // Check the resource ID used when you create the authorization token
//         var resourceId = jwtToken.Claims.FirstOrDefault(c => c.Type is "appid" or "azp")?.Value;
//         var expectedResourceId = Environment.GetEnvironmentVariable("ResourceId");
//         if (resourceId != expectedResourceId)
//         {
//             _logger.LogWarning("Resource ID does not match.");
//             _logger.LogWarning($"Expected Resource ID: {expectedResourceId}");
//             _logger.LogWarning($"Found Resource ID: {resourceId}");
            
//             isValid = false;
//         }

//         return isValid;
//     }
// }
// }

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.Tasks;

namespace WebhookFunctionProj
{
    public class WebhookFunctionProj
    {
        [Function("WebhookFunctionProj")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("WebhookFunctionProj");
            logger.LogInformation("Webhook invoked.");

            // Read request body
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic body = JsonConvert.DeserializeObject(requestBody);

            // JWT Validation
            var jwtValid = JwtIsValid(req, logger);
            if (!jwtValid)
            {
                logger.LogCritical("JWT security checks failed!");
                var response = req.CreateResponse(HttpStatusCode.BadRequest);
                return response;
            }

            // Simulated logic to store payload
            var item = new
            {
                id = Guid.NewGuid().ToString(),
                payload = body
            };

            var okResponse = req.CreateResponse(HttpStatusCode.OK);
            return okResponse;
        }

        private static bool JwtIsValid(HttpRequestData req, ILogger logger)
        {
            if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
            {
                logger.LogCritical("Authorization header is missing.");
                return false;
            }

            var authHeader = authHeaders.First();
            var jwt = authHeader.Split(' ')[1];
            var handler = new JwtSecurityTokenHandler();

            if (!handler.CanReadToken(jwt))
            {
                logger.LogCritical("Can't read JWT");
                return false;
            }

            var jwtToken = handler.ReadToken(jwt) as JwtSecurityToken;
            if (jwtToken == null)
            {
                logger.LogCritical("JWT is missing");
                return false;
            }

            return JwtClaimsAreValid(jwtToken, logger);
        }

        private static bool JwtClaimsAreValid(JwtSecurityToken jwtToken, ILogger logger)
        {
            var isValid = true;

            string expectedClientId = Environment.GetEnvironmentVariable("ClientId");
            string expectedTenantId = Environment.GetEnvironmentVariable("TenantId");
            string expectedIssuer = $"https://sts.windows.net/{expectedTenantId}/";
            string expectedResourceId = Environment.GetEnvironmentVariable("ResourceId");

            string clientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value;
            string tenantId = jwtToken.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;
            string issuer = jwtToken.Claims.FirstOrDefault(c => c.Type == "iss")?.Value;
            string resourceId = jwtToken.Claims.FirstOrDefault(c => c.Type is "appid" or "azp")?.Value;

            if (clientId != expectedClientId)
            {
                logger.LogWarning("Client ID mismatch");
                isValid = false;
            }

            if (tenantId != expectedTenantId)
            {
                logger.LogWarning("Tenant ID mismatch");
                isValid = false;
            }

            if (issuer != expectedIssuer)
            {
                logger.LogWarning("Issuer mismatch");
                isValid = false;
            }

            if (resourceId != expectedResourceId)
            {
                logger.LogWarning("Resource ID mismatch");
                isValid = false;
            }

            return isValid;
        }
    }
}

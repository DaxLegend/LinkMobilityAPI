using LinkMobilityAPI.Models;
using LinkMobilityAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace LinkMobilityAPI.Controllers
{
    [Produces("application/json")]
    [Route("Auth")]
    [ApiController]
    public class AuthController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoDatabase db = database;

        [Route("Session")]
        [HttpPost]
        public ActionResult Session()
        {
            try
            {
                string authToken = Request.Cookies["authToken"];
                string sessionToken = Request.Cookies["sessionToken"];
                if(authToken == null || sessionToken == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }
                string userId = "";
                string username = "";
                
                if (Utils.IsValidAuthToken(authToken, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }
                if (Utils.IsValidSessionToken(sessionToken, ref username) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }
                
                return StatusCode(StatusCodes.Status200OK);
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in Session. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }

        [Route("Login")]
        [HttpPost]
        public ActionResult Login([FromBody] CredentialsData credentials)
        {
            try
            {
                string username = credentials.Username;
                string password = credentials.Password;

                if (username == null || password == null)
                {
                    return StatusCode(StatusCodes.Status400BadRequest);
                }

                string userId = "";
                int expiration = 8;

                if (Utils.CheckCredentials(db, username, password, ref userId) == true)
                {
                    CookieOptions? option = CreateOptionResponse();
                    if (option == null)
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult("Error while creating option response"));
                    }
                    Response.Cookies.Append("authToken", Utils.GenerateTokenUserData(userId, expiration), option);
                    option.HttpOnly = false;
                    Response.Cookies.Append("sessionToken", Utils.GenerateToken(username, expiration), option);
                    return StatusCode(StatusCodes.Status200OK);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in Login. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        private CookieOptions? CreateOptionResponse()
        {
            try
            {
                CookieOptions option = new CookieOptions();
                option.Path = "/";
                string origin = Request.Headers.Origin;
                option.Secure = true;
                option.SameSite = SameSiteMode.Strict;
                option.HttpOnly = true;
                option.Expires = DateTime.UtcNow.AddHours(8);

                return option;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in CreateOptionResponse. Message: {ex.Message}");
                return null;
            }
        }

    }
}

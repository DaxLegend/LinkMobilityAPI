using Amazon.Runtime.Internal;
using LinkMobilityAPI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Diagnostics.Metrics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LinkMobilityAPI.Utilities
{
    public class Utils
    {

        private const string SECRET_KEY = "TQvgjeABMPOwCycOqah5EQu5yyVjpmVG";
        public static readonly SymmetricSecurityKey SIGNING_KEY = new(Encoding.UTF8.GetBytes(SECRET_KEY));

        internal static bool IsValidSessionToken(string authToken, ref string usename)
        {
            try
            {
                JwtSecurityToken security = new JwtSecurityTokenHandler().ReadJwtToken(authToken);
                usename = security.Claims.Where(o => o.Type.Contains("username") || o.Type.Contains("user_id")).Select(o => o.Value).FirstOrDefault();
                if (usename == null)
                    return false;
                if (security.ValidTo >= DateTime.Now)
                {
                    return true;
                }
                else return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in IsValidSessionToken. Message: {ex.Message}");
                return false;
            }
        }

        internal static bool ValidateRequest(HttpRequest request, ref string username, ref string userId)
        {
            try
            {
                string authToken = request.Cookies["authToken"];
                string sessionToken = request.Cookies["sessionToken"];

                if (authToken == null || sessionToken == null)
                {
                    return false;
                }
                if (IsValidAuthToken(authToken, ref userId) == false)
                {
                    return false;
                }
                if (IsValidSessionToken(sessionToken, ref username) == false)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in ValidateRequest. Message: {ex.Message}");
                return false;
            }
        }

        internal static bool IsValidAuthToken(string authToken, ref string userId)
        {
            try
            {
                JwtSecurityToken security = new JwtSecurityTokenHandler().ReadJwtToken(authToken);
                string userid = security.Claims.Where(o => o.Type.Contains("username") || o.Type.Contains("user_id")).Select(o => o.Value).FirstOrDefault();
                if (userid == null)
                    return false;
                userId = userid;
                if (security.ValidTo >= DateTime.Now)
                {
                    return true;
                } else return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in IsValidAuthToken. Message: {ex.Message}");
                return false;
            }
        }

        internal static bool CheckCredentials(IMongoDatabase db, string username, string password, ref string userId)
        {
            try
            {
                var usersCollection = db.GetCollection<BsonDocument>("users");

                // Cerca l'utente nel database
                var filter = Builders<BsonDocument>.Filter.Eq("email", username);
                var user = usersCollection.Find(filter).FirstOrDefault();

                if (user != null)
                {
                    // Recupera la password crittografata dal documento dell'utente
                    var storedPasswordHash = user.GetValue("password").AsString;

                    // Cifra la password fornita dall'utente nello stesso modo della password memorizzata nel database
                    var passwordHash = CalculateHash(password);

                    // Confronta le password cifrate
                    if (storedPasswordHash == passwordHash)
                    {
                        // Le credenziali sono corrette, imposta l'ID utente e restituisci true
                        userId = user.GetValue("_id").AsObjectId.ToString();
                        return true;
                    }
                }

                // Nessun utente trovato con il nome utente fornito o password non corrispondenti
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in CheckCredentials. Message: {ex.Message}");
                return false;
            }
        }

        private static string CalculateHash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Generate a Token with expiration date and Claim meta-data. It signs the token with the SIGNING_KEY
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static string GenerateToken(string username, int hour)
        {
            try
            {
                var token = new JwtSecurityToken(
                claims: [new("username", username)],
                notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                expires: new DateTimeOffset(DateTime.Now.AddHours(hour)).DateTime,
                signingCredentials: new SigningCredentials(SIGNING_KEY, SecurityAlgorithms.HmacSha256));
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in GenerateToken. Message: {ex.Message}");
            }
            return "";
        }

        /// <summary>
        /// Generate a Token with expiration date and Claim meta-data. It signs the token with the SIGNING_KEY
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public static string GenerateTokenUserData(string data, int hour)
        {
            try
            {
                var token = new JwtSecurityToken(
                claims: [new("user_id", data)],
                notBefore: new DateTimeOffset(DateTime.Now).DateTime,
                expires: new DateTimeOffset(DateTime.Now.AddHours(hour)).DateTime,
                signingCredentials: new SigningCredentials(SIGNING_KEY, SecurityAlgorithms.HmacSha256));
                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in GenerateTokenUserData. Message: {ex.Message}");
            }
            return "";
        }


        public static List<Customer> FromBsonToJson(List<BsonDocument> customers, IMongoDatabase db)
        {
            try
            {
                List<Customer> result = new List<Customer>();
                foreach (var customer in customers)
                {
                    var newCustomer = new Customer
                    {
                        Id = customer.GetValue("_id").ToString(),
                        CompanyName = customer.GetValue("companyName").AsString,
                        Address = customer.GetValue("address").AsString,
                        State = customer.GetValue("state").AsString,
                        Country = customer.GetValue("country").AsString,
                        SubscriptionState = customer.GetValue("subscriptionState").AsString,
                        NumberOfInvoices = GetNumberOfInvoicesForCustomer(customer!.GetValue("_id")!.ToString()!, db)
                    };
                    result.Add(newCustomer);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in FromBsonToJson. Message: {ex.Message}");
                return new List<Customer>();
            }
        }

        public static int GetNumberOfInvoicesForCustomer(string customerId, IMongoDatabase db)
        {
            var invoiceCollection = db.GetCollection<BsonDocument>("invoices");
            var filter = Builders<BsonDocument>.Filter.Eq("customerId", customerId);
            var count = invoiceCollection.Find(filter).CountDocuments();
            return (int)count;
        }

    }
}

using Amazon.Runtime.Internal;
using LinkMobilityAPI.Models;
using LinkMobilityAPI.Utilities;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace LinkMobilityAPI.Controllers
{
    [Produces("application/json")]
    [Route("Customers")]
    [ApiController]
    public class CustomersController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoDatabase db = database;

        [Route("GetCustomers")]
        [HttpPost]
        public ActionResult<CustomerData> GetCustomers([FromBody] Paginator filter)
        {
            try
            {
                string username = "";
                string userId = "";
                if (Utils.ValidateRequest(Request, ref username, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                int skip = (filter.First-1) * filter.Rows; // Calcolo il numero di record da saltare
                int limit = filter.Rows; // Numero di record da restituire

                var customersCollection = db.GetCollection<BsonDocument>("customers");

                // Eseguo una query al database per recuperare i clienti paginati e una per il conteggio complessivo dei customers
                var customers = customersCollection
                    .Find(_ => true)
                    .Skip(skip)
                    .Limit(limit)
                    .ToList();

                var totalCustomers = customersCollection
                    .Find(_ => true)
                    .CountDocuments();

                CustomerData res = new()
                {
                    Customers = Utils.FromBsonToJson(customers, db),
                    Total = (int) totalCustomers
                };

                return Ok(res);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in GetCustomers. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }

        [Route("DeleteCustomer")]
        [HttpPost]
        public ActionResult DeleteCustomer([FromBody] Customer customer)
        {
            try
            {
                string username = "";
                string userId = "";
                if (Utils.ValidateRequest(Request, ref username, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }
                string id = customer.Id!.ToString();
                var customersCollection = db.GetCollection<BsonDocument>("customers");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var result = customersCollection.DeleteOne(filter);

                if (result.DeletedCount == 1)
                {
                    return Ok("Customer deleted successfully.");
                }
                else
                {
                    return NotFound("Customer not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in DeleteCustomer. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }

        [Route("GetInvoices")]
        [HttpPost]
        public ActionResult<List<Invoice>> GetInvoices([FromBody] Customer customer)
        {
            try
            {
                string username = "";
                string userId = "";
                if (Utils.ValidateRequest(Request, ref username, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                var invoicesCollection = db.GetCollection<BsonDocument>("invoices");
                var filter = Builders<BsonDocument>.Filter.Eq("customerId", customer!.Id);

                var invoices = invoicesCollection.Find(filter).ToList();
                var invoiceList = new List<Invoice>();

                foreach (var invoice in invoices)
                {
                    Invoice invoiceObj = new()
                    {
                        InvoiceNumber = invoice["invoiceNumber"].AsString,
                        Date = invoice["date"].AsString,
                        Total = invoice["total"].AsInt32,
                        CustomerId = invoice["customerId"].AsString
                    };
                    invoiceList.Add(invoiceObj);
                }

                return Ok(invoiceList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in GetInvoices. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }

        [Route("AddCustomer")]
        [HttpPost]
        public ActionResult AddCustomer([FromBody] Customer customer)
        {
            try
            {
                string username = "";
                string userId = "";
                if (Utils.ValidateRequest(Request, ref username, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                var customersCollection = db.GetCollection<BsonDocument>("customers");

                var document = new BsonDocument
                {
                    { "companyName", customer.CompanyName },
                    { "address", customer.Address },
                    { "state", customer.State },
                    { "country", customer.Country },
                    { "subscriptionState", customer.SubscriptionState }
                };

                customersCollection.InsertOne(document);

                return Ok("Customer added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in AddCustomer. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }

        [Route("EditCustomer")]
        [HttpPost]
        public ActionResult EditCustomer([FromBody] CustomerChanges customerChanges)
        {
            try
            {
                string username = "";
                string userId = "";
                if (Utils.ValidateRequest(Request, ref username, ref userId) == false)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized);
                }

                var customersCollection = db.GetCollection<BsonDocument>("customers");
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(customerChanges.Id));

                foreach (var change in customerChanges.Changes!)
                {
                    var update = Builders<BsonDocument>.Update.Set(change.Field, change.Value);
                    customersCollection.UpdateOne(filter, update);
                }

                return Ok("Customer updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception thrown in EditCustomer. Message: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError, new JsonResult(ex.Message));
            }
        }
    }

}

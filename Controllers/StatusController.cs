using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SystemAdmin.Context;
using SystemAdmin.Models;
using System.Linq.Dynamic.Core;

namespace SystemAdmin.Controllers
{
    public class StatusController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly string _baseUrl;

        public StatusController(HttpClient httpClient, IConfiguration config, AppDbContext context)
        {
            _httpClient = httpClient;
            _config = config;
            _baseUrl = _config.GetValue<string>("Urls:BaseUrl") ?? "";
        }

        public IActionResult Index()
        {
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "";
            ViewData["baseUrl"] = _baseUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetData()
        {
            try
            {
                // Get parameters from request
                var request = Request.Form;
                var draw = request["draw"].FirstOrDefault();
                var start = int.Parse(request["start"].FirstOrDefault() ?? "0");
                var length = int.Parse(request["length"].FirstOrDefault() ?? "10");
                var searchValue = request["search[value]"].FirstOrDefault();
                var startDateStr = request["startDate"].FirstOrDefault();
                var endDateStr = request["endDate"].FirstOrDefault();

                // Sorting parameters
                var sortColumnIndex = int.Parse(request["order[0][column]"].FirstOrDefault() ?? "0");
                var sortColumnName = request[$"columns[{sortColumnIndex}][data]"].FirstOrDefault() ?? "ParentEvent.Title";
                var sortDirection = request["order[0][dir]"].FirstOrDefault() ?? "asc";

                // Calculate page number for the API (DataTables uses start/length)
                var pageNumber = (start / length) + 1;
                // Construct the API URL with query parameters
                var queryParams = new List<string>
                {
                    $"pageSize={length}",
                    $"pageNumber={pageNumber}",
                    $"sortColumn={Uri.EscapeDataString(sortColumnName)}",
                    $"sortDirection={Uri.EscapeDataString(sortDirection)}"
                };
                if (!string.IsNullOrEmpty(searchValue))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchValue)}");

                if (!string.IsNullOrEmpty(startDateStr))
                    queryParams.Add($"startDate={Uri.EscapeDataString(startDateStr)}");

                if (!string.IsNullOrEmpty(endDateStr))
                    queryParams.Add($"endDate={Uri.EscapeDataString(endDateStr)}");
                var apiUrl = $"{_baseUrl}/api/getevents?{string.Join("&", queryParams)}";
                // Make the API call
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();
                // Read and deserialize the response
                var content = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonConvert.DeserializeObject<IntegrationEventContent>(content);
                // Format the response for DataTables
                var groupedData = apiResponse?.Data
                    .GroupBy(item => item.Identifier) // Grouping by Identifier
                    .Select(group =>
                    {
                        var parentEvent = group.FirstOrDefault()?.ParentEvent; // Get the ParentEvent
                        var relatedEvents = group.Skip(1).Select(item => new
                        {
                            item.Identifier,
                            ParentEvent = item.ParentEvent // These are the related events
                        }).ToList();

                        return new
                        {
                            Identifier = group.Key,
                            ParentEvent = parentEvent,
                            RelatedEvents = relatedEvents
                        };
                    }).ToList();

                // Format the response for DataTables
                var jsonData = new
                {
                    draw = draw ?? "0",
                    recordsFiltered = apiResponse?.TotalRecords ?? 0,
                    recordsTotal = apiResponse?.TotalRecords ?? 0,
                    data = groupedData
                };

                return new JsonResult(jsonData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEvent(string eventId)
        {
            Guid guidValue;
            if (!Guid.TryParse(eventId, out guidValue))
            {
                return Json(new { success = false, message = "Invalid EventId." });
            }

            try
            {
                // Construct the API URL for deleting the event
                var apiUrl = $"{_baseUrl}/api/deleteevent/{guidValue}";

                // Make the HTTP DELETE request to the API
                var response = await _httpClient.DeleteAsync(apiUrl);

                // Check if the response is successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = errorMessage });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log the exception and return an error message
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
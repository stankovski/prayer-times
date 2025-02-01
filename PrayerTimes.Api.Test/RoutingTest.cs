using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace PrayerTimes.Api.Test
{
    public class RoutingTest : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RoutingTest(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetTimes_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/prayer-times/2024-01-01", new StringContent("{}", Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetTimesRange_WithValidDates_ReturnsOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "token");

            var requestBody = "{" +
                "\"fromDate\": \"2025-01-01T00:00:00Z\"," +
                "\"toDate\": \"2025-02-01T00:00:00Z\"," +
                "\"timeZone\": -8," +
                "\"latitude\": 47.771610," +
                "\"longitude\": -122.194168," +
                "\"calculationMethod\": \"ISNA\"," +
                "\"asrJuristicMethod\": \"Shafii\"," +
                "\"highLatitudeAdjustmentMethod\": \"None\"" +
            "}";

            // Act
            var response = await client.PostAsync(
                "/api/prayer-times/range?apiVersion=1.0",
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var stringResponse = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(stringResponse); // Check if the response body is not empty

            var prayerTimes = JsonConvert.DeserializeObject<PrayerTimesResponseRange>(stringResponse);
            Assert.Equal(32, prayerTimes.PrayerTimes.Count); // Check for expected number of days
        }
        
        [Fact]
        public async Task GetTimesRange_WithInvalidDates_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "token");

            var requestBody = "{" +
                "\"fromDate\": \"2025-02-01T00:00:00Z\"," + // FromDate is after ToDate
                "\"toDate\": \"2025-01-01T00:00:00Z\"," +
                "\"timeZone\": -8," +
                "\"latitude\": 47.771610," +
                "\"longitude\": -122.194168," +
                "\"calculationMethod\": \"ISNA\"," +
                "\"asrJuristicMethod\": \"Shafii\"," +
                "\"highLatitudeAdjustmentMethod\": \"None\"" +
            "}";

            // Act
            var response = await client.PostAsync(
                "/api/prayer-times/range?apiVersion=1.0",
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetTimesRange_WithMissingParameters_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "token");

            var requestBody = "{}"; // Empty body - missing all parameters

            // Act
            var response = await client.PostAsync(
                "/api/prayer-times/range?apiVersion=1.0",
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetTimesRange_WithMissingApiVersion_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "token");

            var requestBody = "{" +
                "\"fromDate\": \"2025-01-01T00:00:00Z\"," +
                "\"toDate\": \"2025-02-01T00:00:00Z\"," +
                "\"timeZone\": -8," +
                "\"latitude\": 47.771610," +
                "\"longitude\": -122.194168," +
                "\"calculationMethod\": \"ISNA\"," +
                "\"asrJuristicMethod\": \"Shafii\"," +
                "\"highLatitudeAdjustmentMethod\": \"None\"" +
            "}";

            // Act
            var response = await client.PostAsync(
                "/api/prayer-times/range",
                new StringContent(requestBody, Encoding.UTF8, "application/json"));

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
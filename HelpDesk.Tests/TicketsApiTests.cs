using System.Net;
using System.Net.Http.Json;
using HelpDesk.Core.Interfaces;
using HelpDesk.Tests.Models;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HelpDesk.Tests;

public class TicketsApiTests : IClassFixture<HelpDeskApiFactory>
{
    private readonly HttpClient _client;
    private readonly HelpDeskApiFactory _factory;
    
    public TicketsApiTests(HelpDeskApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");
    }

    [Fact]
    public async Task GetAll_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        // Nothing to arrange
        
        // Act
        var response = await _client.GetAsync("/api/Tickets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithMissingDescription_ReturnsBadRequestWithErrors()
    {
        // Arrange
        var body = new { id = 60, user = "test user" };

        // Act
        var response = await _client.PostAsJsonAsync("api/Tickets", body);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"errors\"", await response.Content.ReadAsStringAsync());
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errors\"", content);
        Assert.Contains("\"Description\"", content);
    }

    [Fact]
    public async Task GetAll_WhenRepositoryThrows_ReturnsProblemDetails_WithoutDetails()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ITicketRepository, ThrowingTicketRepository>();
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("X-Api-Key", "test-key");

        // Act
        var response = await client.GetAsync("/api/Tickets");
        
        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"traceId\"", content);
        Assert.DoesNotContain("Secret database details", content);
        Assert.DoesNotContain("ThrowingTicketRepository", content);
    }
}
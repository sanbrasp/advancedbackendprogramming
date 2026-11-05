using System.Text.Json.Serialization;
using HelpDesk.Api.Data;
using HelpDesk.Api.Filters;
using HelpDesk.Api.Interfaces;
using HelpDesk.Api.Middleware;
using HelpDesk.Api.Models;
using HelpDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepositoryRepository>();
builder.Services.AddScoped<AuditActionFilter>();
builder.Services.AddScoped<ValidateIdFilter>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddTransient<IOperationTransient, Operation>();
builder.Services.AddScoped<IOperationScoped, Operation>();
builder.Services.AddSingleton<IOperationSingleton, Operation>();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
        options.UseRequestInterceptor("(req) => { req.headers['X-Api-Key'] = 'anything'; return req; }");
    });
}

app.UseExceptionHandler("/error");

app.UseRequestTiming();

app.UseMiddleware<ApiKeyMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
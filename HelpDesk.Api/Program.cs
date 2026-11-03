using System.Text.Json.Serialization;
using HelpDesk.Api.Data;
using HelpDesk.Api.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepositoryRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => 
        options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"A in {context.Request.Path}");
    
    await next.Invoke(context);
    
    Console.WriteLine("A out");
});
app.Use(async (context, next) =>
{
    Console.WriteLine("B in");

    await next.Invoke(context);

    Console.WriteLine("B out");
});

app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
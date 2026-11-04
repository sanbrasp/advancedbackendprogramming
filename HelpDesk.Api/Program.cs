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

// Middleware testing
// app.Use(async (context, next) =>
// {
//     Console.WriteLine($"A in {context.Request.Path}"); // "A in" runs fine, and moves to await next.
//     await next.Invoke(context); // Calls "B in"
//     
//     Console.WriteLine("A out"); // From "B out", control goes back to "A", prints "A out".
// });
// app.Use(async (context, next) =>
// {
//     Console.WriteLine($"B in {context.Request.Path}"); // "B in" runs, and moves to await next.
//
//     await next.Invoke(context); // Calls "C in"
//
//     Console.WriteLine("B out"); // after "return" in "C", control comes back to B, prints "B out" - moves to A.
// });
// app.Use(async (context, next) =>
// {
//     Console.WriteLine("C in"); // "C in" runs, then it requests a context Header "X-Api-key", which does not exist:
//     if (!context.Request.Headers.ContainsKey("X-Api-Key")) // this will never make it to "next", short-circuiting the chain
//     {
//         context.Response.StatusCode = 401;
//         return; // From here "C" is done, and control goes back to "B".
//     }
//     await next.Invoke(context); // this never fires when the header is missing
//     Console.WriteLine("C out"); // "C out" never runs if header is missing. Neither does the following middleware
// });
//     
// Above, for testing

app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
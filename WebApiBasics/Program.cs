using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => 
        options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseExceptionHandler("/Error");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

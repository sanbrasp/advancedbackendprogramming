using System.Text.Json.Serialization;
using FluentValidation;
using HelpDesk.Api.DTOs;
using HelpDesk.Api.ExceptionHandlers;
using HelpDesk.Api.Filters;
using HelpDesk.Api.Interfaces;
using HelpDesk.Api.Middleware;
using HelpDesk.Api.Models;
using HelpDesk.Api.Settings;
using HelpDesk.Api.Validators;
using HelpDesk.Core.Interfaces;
using HelpDesk.Core.Services;
using HelpDesk.Infrastructure.Data;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => 
        lc.ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict);
    
    builder.Services.AddOpenApi();
    builder.Services.ConfigureHttpJsonOptions(o =>
        o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
    
    builder.Services.AddSingleton<ITicketRepository, InMemoryTicketRepository>();
    builder.Services.AddScoped<AuditActionFilter>();
    builder.Services.AddScoped<ValidateIdFilter>();
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddTransient<IOperationTransient, Operation>();
    builder.Services.AddScoped<IOperationScoped, Operation>();
    builder.Services.AddSingleton<IOperationSingleton, Operation>();
    builder.Services.AddOptions<TicketSettings>()
        .Bind(builder.Configuration.GetSection(TicketSettings.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    
    builder.Services.AddOptions<SecuritySettings>()
        .Bind(builder.Configuration.GetSection(SecuritySettings.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddScoped<IValidator<CreateTicketRequestDto>, CreateTicketRequestDtoValidator>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        var apiKey = app.Configuration["Security:ApiKey"];

        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "v1");
            options.UseRequestInterceptor(
                $"(req) => {{ req.headers['X-Api-Key'] = '{apiKey}'; return req; }}");
        });
    }

    app.UseExceptionHandler();

    app.UseRequestTiming();

    app.UseMiddleware<ApiKeyMiddleware>();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
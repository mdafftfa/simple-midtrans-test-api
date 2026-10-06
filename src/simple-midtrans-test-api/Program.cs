using Carter;
using DotNetEnv;
using Scalar.AspNetCore;
using Sindika.AspNet.Midtrans.Configuration;
using Sindika.AspNet.Midtrans.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddMidtrans((options) =>
{
    options.ClientKey = Environment.GetEnvironmentVariable("MIDTRANS_CLIENT_KEY");
    options.IsProduction = false;
    options.ServerKey = Environment.GetEnvironmentVariable("MIDTRANS_SERVER_KEY");
    options.Snap = new SnapOptions
    {
        TimeoutInSeconds = 86400,
    };
});

var app = builder.Build();

app.MapCarter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "openapi/{documentName}.json";
    });

    app.MapScalarApiReference("/", options =>
    {
        options.WithOpenApiRoutePattern("/openapi/v1.json");
    });
}

app.Run();
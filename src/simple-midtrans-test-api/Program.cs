using Carter;
using DotNetEnv;
using Sindika.AspNet.Midtrans.Configuration;
using Sindika.AspNet.Midtrans.Extensions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

builder.Configuration.AddEnvironmentVariables();
builder.Services.AddCarter();
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

app.MapCarter();
app.Run();
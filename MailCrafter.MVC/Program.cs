using MailCrafter.Domain.Repositories;
using MailCrafter.Infrastructure.Repositories;
using MailCrafter.MVC.Jobs;
using MailCrafter.Repositories;
using MailCrafter.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "UserLoginCookie";
        config.LoginPath = "/login";
    });
var configuration = new ConfigurationBuilder()
           .AddJsonFile(@"C:\MailCrafter\Development\Core\appsettings.Development.json", optional: true, reloadOnChange: true)
           .Build();
builder.Services.AddSingleton<IConfiguration>(configuration);

// Register MongoDB client and repositories
builder.Services.AddCoreServices();

// Configure Quartz Jobs for scheduled emails
builder.Services.ConfigureQuartzJobs();

builder.Services.AddSingleton<ConnectionMonitorService>();
builder.Services.AddSingleton<WebSocketConnectionManager>();
builder.Services.AddHostedService<ConnectionMonitorService>();
builder.Services.AddScoped<IRobotRepository, RobotRepository>();
builder.Services.AddScoped<IBotPackageRepository, BotPackageRepository>();

// Fix: Register the concrete implementation of IFileStorageService
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddScoped<IBotPackageService, BotPackageService>();


// Register services
builder.Services.AddScoped<RobotService>();

// Configure WebSocket options
builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromMinutes(2);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:3000") // Add your React app's URL
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); // Important for cookies/auth
        });
});
var app = builder.Build();
app.UseCors(options =>
    options.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()
           .WithExposedHeaders("Content-Disposition"));
app.UseCors("AllowAll");
// Apply WebSockets middleware (only once)
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
    AllowedOrigins = { "*" } // Or specify your allowed origins
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.Run();

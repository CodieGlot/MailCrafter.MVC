using MailCrafter.Services;
using MailCrafter.Worker;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "UserLoginCookie";
        config.LoginPath = "/login";
    });
builder.Services.AddHttpContextAccessor();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Conditionally add local-only dev settings
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        @"C:\MailCrafter\Development\Core\appsettings.Development.json",
        optional: true,
        reloadOnChange: true);
}

// Add env vars
builder.Configuration.AddEnvironmentVariables();

// Register MongoDB client and repositories
builder.Services.AddCoreServices();

// Add MVCWorker
builder.Services.AddHostedService<MVCWorker>();
builder.Services.AddSingleton<MVCTaskQueueInstance>();
builder.Services.AddTaskHandlers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.Run();


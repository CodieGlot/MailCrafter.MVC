using MailCrafter.Services;


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


var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

app.Run();


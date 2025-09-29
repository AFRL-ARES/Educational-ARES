using Ares.Core.AresEnvironment;
using Ares.Datamodel.Templates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UI;
using UI.Backend.Helpers;
using UI.Data;
using UI.Services.Grpc;
using UI.Services.Notification;
using UI.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if(connectionString is null)
  throw new InvalidOperationException("Connection string was null!");

var provider = builder.Configuration.Get<AppSettings>()!.DatabaseProvider;

switch(provider)
{
  case "SqlServer":
    builder.Services.AddDbContextFactory<ApplicationDbContext>(dbBuilder =>
    {
      dbBuilder.UseSqlServer(builder.Configuration!.GetConnectionString("DefaultConnection"));
      dbBuilder.EnableSensitiveDataLogging();
    });
    break;
  case "Sqlite":
    builder.Services.AddDbContextFactory<ApplicationDbContext>(dbBuilder =>
    {
      dbBuilder.UseSqlite(builder.Configuration!.GetConnectionString("DefaultConnection"));
      dbBuilder.EnableSensitiveDataLogging();
    });
    break;
  default:
    throw new InvalidOperationException("FIX MEEEEE");
}
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
  options.SignIn.RequireConfirmedAccount = true;
  options.Password = new PasswordOptions
  {
    RequireDigit = false,
    RequiredLength = 6,
    RequiredUniqueChars = 0,
    RequireLowercase = false,
    RequireNonAlphanumeric = false,
    RequireUppercase = false
  };
})
  .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.Configure<RemoteServiceSettings>(builder.Configuration.GetSection(nameof(RemoteServiceSettings)));
builder.Services.Configure<CertificateSettings>(builder.Configuration.GetSection(nameof(CertificateSettings)));

builder.Services.AddScoped<IClientManager, ClientManager>();
builder.Services.LoadAresModules();
builder.Services.BindClients();
builder.Services.AddSingleton<INotificationReceivingService, NotificationReceivingService>();

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

builder.Services.AddHostedService<ServiceStarter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
  app.UseMigrationsEndPoint();
}
else
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Services.GetService<UnitCategoryHelper>();

app.Run();
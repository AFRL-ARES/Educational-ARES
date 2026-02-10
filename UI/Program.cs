using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Serilog;
using UI;
using UI.Backend.Helpers;
using UI.Data;
using UI.Services.Grpc;
using UI.Services.Notification;
using UI.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

Log.Logger = new LoggerConfiguration()
  .CreateBootstrapLogger();

builder.Configuration
  .AddJsonFile("appsettings.ui.json", optional: false, reloadOnChange: true)
  .AddJsonFile($"appsettings.ui.{builder.Environment.EnvironmentName}.json", optional: true);

ConfigureDatabaseServices(builder.Services, builder.Configuration);

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
}).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddServerSideBlazor();
builder.Services.AddRadzenComponents();
builder.Services.Configure<RemoteServiceSettings>(builder.Configuration.GetSection(nameof(RemoteServiceSettings)));
builder.Services.Configure<CertificateSettings>(builder.Configuration.GetSection(nameof(CertificateSettings)));

builder.Services.AddSingleton<IMessenger>(provider => new WeakReferenceMessenger());
builder.Services.AddScoped<IClientManager, ClientManager>();
builder.Services.LoadAresModules();
builder.Services.BindClients();
builder.Services.AddSingleton<INotificationReceivingService, NotificationReceivingService>();

builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

builder.Services.AddHostedService<ServiceStarter>();

builder.Services.AddSerilog((services, lc) => lc
  .ReadFrom.Configuration(builder.Configuration)
  .ReadFrom.Services(services)
  .Enrich.FromLogContext());

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

static void ConfigureDatabaseServices(IServiceCollection services, ConfigurationManager configuration)
{
  var sqlConnectionStrings = configuration.GetSection("ConnectionStrings");
  var provider = configuration.Get<AppSettings>().DatabaseProvider ?? "Sqlite";

  if(provider == "SqlServer")
  {
    services.AddDbContextFactory<ApplicationDbContext>(b =>
    {
      b.UseSqlServer(sqlConnectionStrings[provider]);
      b.EnableSensitiveDataLogging();
    });
  }
  else if(provider == "Sqlite")
  {
    services.AddDbContextFactory<ApplicationDbContext>(b =>
    {
      b.UseSqlite(sqlConnectionStrings[provider]);
      b.EnableSensitiveDataLogging();
    });
  }
  else if(provider == "Postgres")
  {
    services.AddDbContextFactory<ApplicationDbContext>(b =>
    {
      b.UseNpgsql(sqlConnectionStrings[provider]);
      b.EnableSensitiveDataLogging();
    });
  }
  else
  {
    throw new InvalidOperationException($"Unsupported database provider: {provider}. Available provider values: {string.Join(',', sqlConnectionStrings.AsEnumerable().Select(scs => scs.Key.Split(':').LastOrDefault()).Where(s => s != "ConnectionStrings"))}");
  }
}
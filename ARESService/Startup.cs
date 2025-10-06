using System;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Ares.Core;
using Ares.Core.Grpc;
using Ares.Datamodel;
using Ares.Services;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AresService;

public class Startup
{
  public Startup(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  private IConfiguration Configuration { get; }

  // This method gets called by the runtime. Use this method to add services to the container.
  // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddGrpc(options => options.EnableDetailedErrors = true);
    services.Configure<TokensConfig>(Configuration.GetSection(nameof(TokensConfig)));
    services.AddLogging(builder => builder.AddConsole());

    var sqlConnectionString = Configuration.GetConnectionString("CoreDatabase");
    var provider = string.Empty;

    if(Configuration is null)
      throw new InvalidOperationException("Configuration cannot be null!");

    provider = Configuration.Get<AppSettings>()!.DatabaseProvider;

    if(provider == "SqlServer")
    {
      services.AddDbContextFactory<AresDbContext>(builder =>
      {
        builder.UseSqlServer(Configuration!.GetConnectionString("CoreDatabase"));
        builder.EnableSensitiveDataLogging();
      });

      services.AddDbContextFactory<AresIdentityContext>(builder => builder.UseSqlServer(sqlConnectionString), ServiceLifetime.Transient);
    }

    else if(provider == "Sqlite")
    {
      services.AddDbContextFactory<AresDbContext>(builder =>
      {
        builder.UseSqlite(sqlConnectionString);
        builder.EnableSensitiveDataLogging();
      });

      services.AddDbContextFactory<AresIdentityContext>(builder => builder.UseSqlite(sqlConnectionString), ServiceLifetime.Transient);
    }

    else
    {
      throw new InvalidOperationException($"Database provider {provider} is not a supported provider for ARES. Use Sqlite, SqlServer or Postgres.");
    }

    services.AddTransient<IDbContextFactory<CoreDatabaseContext>>(provider
      => new CovariantCoreDbContextFactory<CoreDatabaseContext, AresDbContext>(provider.GetRequiredService<IDbContextFactory<AresDbContext>>()));

    var identityBuilder = services.AddIdentityCore<AresUser>(o =>
      o.Password = new PasswordOptions
      {
        RequireDigit = false,
        RequiredLength = 6,
        RequiredUniqueChars = 0,
        RequireLowercase = false,
        RequireNonAlphanumeric = false,
        RequireUppercase = false
      });// TODO maybe make password requirements more stringent?

    identityBuilder = new IdentityBuilder(identityBuilder.UserType, typeof(IdentityRole), identityBuilder.Services);
    identityBuilder.AddEntityFrameworkStores<AresIdentityContext>();
    identityBuilder.AddRoleValidator<RoleValidator<IdentityRole>>();
    identityBuilder.AddRoleManager<RoleManager<IdentityRole>>();
    identityBuilder.AddSignInManager<SignInManager<AresUser>>();
    identityBuilder.AddDefaultTokenProviders();

    var token = Configuration?.Get<AppSettings>()?.TokensConfig?.Key ?? "DefaultKey";
    var key = Encoding.ASCII.GetBytes(token);

    //var certPath = Configuration.GetRequiredSection("CertificateSettings")["Path"];
    //var certPassword = Configuration.GetRequiredSection("CertificateSettings")["Password"];

    //services.Configure<KestrelServerOptions>(options => {
    //  options.ConfigureHttpsDefaults(o => {
    //    o.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
    //    o.CheckCertificateRevocation = false;
    //    o.ClientCertificateValidation = ClientCertificateValidation;
    //    o.ServerCertificate = new X509Certificate2(certPath, certPassword);
    //  });
    //});

    // lets allow both the JWT auth and certificate auth support for now with JWT being optional
    services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddJwtBearer(o =>
    {
      o.RequireHttpsMetadata = false;
      o.SaveToken = true;
      o.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        RequireExpirationTime = false
      };
    }).AddCertificate(o =>
    {
      o.AllowedCertificateTypes = CertificateTypes.All;
      o.RevocationMode = X509RevocationMode.NoCheck;
    });

    services.AddAuthorization(o => o.AddPolicy("AresPolicy", builder => builder.RequireRole(Enum.GetNames<AresUserType>())));

    services.AddAres(Configuration);
  }

  private void PopulateAresConfig()
  {
    var basePath = Configuration.Get<AppSettings>().AresDataPath;

    AresConfig.ResultsPath = Path.Combine(basePath, AppSettings.ResultsFolder);
    AresConfig.TemplatePath = Path.Combine(basePath, AppSettings.TemplatesFolder);
    AresConfig.DevicesPath = Path.Combine(basePath, AppSettings.DevicesFolder);
    AresConfig.TagsPath = Path.Combine(basePath, AppSettings.ExperimentTagsFile);
  }

  private bool ClientCertificateValidation(X509Certificate2 arg1, X509Chain? arg2, SslPolicyErrors arg3)
  {
    // TODO this might need to be revisited?
    return true;
    //var subjectData = arg1.SubjectName.Name.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
    //if (!subjectData.Contains("CN=FC2Client"))
    //  return false;

    //var issuerSubjectData = arg1.IssuerName.Name.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
    //if (!issuerSubjectData.Contains("CN=FC2Root"))
    //  return false;

    //var certPath = Configuration.GetSection("CertificateSettings")["Path"];
    //var certPassword = Configuration.GetSection("CertificateSettings")["Password"];

    //var serviceCert = new X509Certificate2(certPath, certPassword);
    //var issuerServiceCert = GetIssuerCert(serviceCert);
    //var issuerClientCert = GetIssuerCert(arg1, arg2);

    //// even if self-signed, as long as the client and the server certificates were signed by the same cert
    //// we can go ahead and say that they're valid for authentication
    //return issuerClientCert?.Thumbprint == issuerServiceCert?.Thumbprint;
  }

  private static X509Certificate2? GetIssuerCert(X509Certificate2 cert, X509Chain? chain = null)
  {
    if(chain is null)
    {
      chain = new X509Chain();
      chain.Build(cert);
    }

    var issuerCert = chain.ChainElements.FirstOrDefault(element => element.Certificate.SubjectName.Name == cert.IssuerName.Name);
    return issuerCert?.Certificate;
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app,
    IWebHostEnvironment env,
    IHostApplicationLifetime applicationLifetime,
    AresStarter starter,
    RoleManager<IdentityRole> roleManager)
  {
    PopulateAresConfig();

    if(env.IsDevelopment())
      app.UseDeveloperExceptionPage();
    else
      app.UseHsts();

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    //User data path has to exist for database creation
    _ = starter.EnsureDataPathsExist();

    var contextFactory = app.ApplicationServices.GetService<IDbContextFactory<AresDbContext>>();
    var context = contextFactory!.CreateDbContext();
    context.Database.Migrate();

    var idContextFactory = app.ApplicationServices.GetService<IDbContextFactory<AresIdentityContext>>();
    var idContext = idContextFactory!.CreateDbContext();
    idContext.Database.Migrate();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapCoreAresServices();
      endpoints.MapAresServices();

      endpoints.MapGet("/",
        async context =>
        {
          await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
        });
    });

    applicationLifetime.ApplicationStopped.Register(OnStopped);
    applicationLifetime.ApplicationStopping.Register(OnStopping);

    roleManager.InitializeAsync().Wait();
    SetupExceptionHandling();
    _ = starter.Start();
  }

  private void OnStopping()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopping, StatusMessage = "Server is stopping." });
  }

  private void OnStopped()
  {
    ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Stopped, StatusMessage = "Server has been stopped." });
  }

  private void SetupExceptionHandling()
  {
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
      LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

    TaskScheduler.UnobservedTaskException += (s, e) =>
    {
      LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
      e.SetObserved();
    };
  }


  private void LogUnhandledException(Exception exception, string source)
  {
    var message = $"Unhandled exception ({source})";
    try
    {
      var assemblyName = Assembly.GetExecutingAssembly().GetName();
      message = string.Format("Unhandled exception in {0} v{1}\n", assemblyName.Name, assemblyName.Version);
      message += exception.Message;
      ServerStatusHelper.ServerStatusSubject.OnNext(new ServerStatusResponse { ServerStatus = ServerStatus.Error, StatusMessage = message });
    }
    catch(Exception)
    {
      // _logger.Error(ex, "Exception in LogUnhandledException");
    }
  }
}

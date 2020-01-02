using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders.InMemory;

namespace dotnet_demoapp
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      // The following line enables Application Insights telemetry collection.
      services.AddApplicationInsightsTelemetry();

      services.AddHttpClient();

      // Make AzureAd optional, if config is missing, skip it
      if (Configuration.GetSection("AzureAd").Exists())
      {

        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = context => false;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        // Sign-in users with the Microsoft identity platform
        services.AddMicrosoftIdentityPlatformAuthentication(Configuration)
        .AddMsal(Configuration, new string[] { "User.Read" })
        .AddInMemoryTokenCaches();

        services.AddRazorPages().AddRazorPagesOptions(options =>
        {
            options.Conventions.AuthorizePage("/User");
        });          

        /*services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
        .AddAzureAD(options => {
          // Force the use of the 'common' endpoints for the STS etc.
          options.Instance = "https://login.microsoftonline.com/common";
          Configuration.Bind("AzureAd", options);
        });

        services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options => {
          // Force use of v2 endpoint, this changes a lot of things in the claims we get
          // But also allows us to sign in with a mix of accounts
          options.Authority = options.Authority + "/v2.0/";

          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = false,
            // With v2 endpoints the name will be blank, so use the preferred_username claim instead
            //NameClaimType = "email"
            NameClaimType = "preferred_username"
          };

          options.Events = new OpenIdConnectEvents
          {
            OnTicketReceived = context => {
              //Console.WriteLine(context);
              // If your authentication logic is based on users then add your logic here
              return Task.CompletedTask;
            },
            OnAuthenticationFailed = context => {
              context.Response.Redirect("/Error");
              context.HandleResponse(); // Suppress the exception
              return Task.CompletedTask;
            },
            OnTokenValidated = context => {
              return Task.CompletedTask;
            }
          };
        });

        services.AddRazorPages().AddRazorPagesOptions(options => {
          options.Conventions.AuthorizePage("/User");
        });

        services.AddRazorPages();
        */
      }
      else
      {
        Console.Out.WriteLine("### AzureAd: Not enabled, configuration settings missing");
        services.AddRazorPages();
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
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

      app.UseStatusCodePages("text/html", "<h1>Something went wrong!</h1>HTTP status code: {0}<br><br><a href='/'>Return to app</a>"); 

      // AzureAd config is optional
      if (Configuration.GetSection("AzureAd").Exists())
      {
        logger.LogInformation("### AzureAd: Enabled with client id: " + Configuration.GetValue<string>("AzureAd:ClientId"));
        app.UseAuthentication();
        app.UseAuthorization();
      }
      else
      {
        logger.LogInformation("### AzureAd: Disabled");
      }

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapRazorPages();
        endpoints.MapControllers();
      });
    }
  }
}
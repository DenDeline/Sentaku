using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Project.ApplicationCore;
using Project.ApplicationCore.Entities;
using Project.ApplicationCore.Interfaces;
using Project.Infrastructure.Data;
using Project.WebMVC.AuthServer;

namespace Project.WebMVC
{
  public class Startup
  {
    public Startup(IConfiguration configuration, IWebHostEnvironment environment)
    {
      Configuration = configuration;
      Environment = environment;
    }

    public const string NetJsClientCorsPolicy = "NetJSClientCorsPolicy";

    public IConfiguration Configuration { get; }
    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddApplicationCore();

      services.AddAutoMapper(typeof(Startup).Assembly);

      services.AddDbContext<AppDbContext>(options =>
      {
        if (Environment.IsDevelopment())
        {
          options.EnableSensitiveDataLogging();
        }
        options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
      });

      services.AddScoped<IDbContext, AppDbContext>();

      services.AddIdentity<AppUser, IdentityRole<int>>(config =>
      {
        config.Password.RequireDigit = false;
        config.Password.RequiredLength = 4;
        config.Password.RequireLowercase = false;
        config.Password.RequireUppercase = false;
        config.Password.RequiredUniqueChars = 1;
        config.Password.RequireNonAlphanumeric = false;
      })
        .AddDefaultUI()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

      services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddGoogle(config =>
        {
          IConfigurationSection googleConfig = Configuration.GetSection("Authentication:Google");

          config.ClientId = googleConfig["ClientId"];
          config.ClientSecret = googleConfig["ClientSecret"];
        })
        .AddJwtBearer(options =>
        {
          var secretKey = new SigningIssuerCertificate().GetPublicKey();

          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidIssuer = "https://localhost:44307",
            ValidAudience = "https://localhost:44307",
            IssuerSigningKey = secretKey,
          };
      });

      services.AddCors(options =>
      {
        options.AddPolicy(NetJsClientCorsPolicy, builder =>
        {
          builder.AllowAnyHeader();
          builder.WithOrigins("http://localhost:3000");
          builder.AllowAnyMethod();
        });
      });

      services.AddControllersWithViews();
      services.AddRazorPages();

      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Vote WebAPI", Version = "v1" });
        c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme()
        {
          Type = SecuritySchemeType.OAuth2,
          Flows = new OpenApiOAuthFlows()
          {
            AuthorizationCode = new OpenApiOAuthFlow()
            {
              AuthorizationUrl = new Uri("/oauth2/authorize", UriKind.Relative),
              TokenUrl = new Uri("/oauth2/token", UriKind.Relative)
            }
          }
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new List<string>()
          }
        });
      });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseDatabaseErrorPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();  
      }
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseCors(NetJsClientCorsPolicy);

      app.UseSwagger();
      app.UseSwaggerUI(c =>
      {
        c.OAuthClientId("project_swagger_3e1db73b647f43c297594797d62aec76");
        c.OAuthUsePkce();
        c.SwaggerEndpoint("v1/swagger.json", "My API V1");
      });
      
      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapDefaultControllerRoute();
        endpoints.MapRazorPages();
      });
    }
  }
}

using ChannelAdvisorLibrary;
using EmailSenderLibrary;
using FileReaderLibrary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewAgeUI.Models;
using NewAgeUI.Utilities;
using SkuVaultLibrary;
using System;

namespace NewAgeUI
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
      string server = Environment.GetEnvironmentVariable("DBHOST") ?? "localhost";
      string port = Environment.GetEnvironmentVariable("DBPORT") ?? "3306";
      string password = Environment.GetEnvironmentVariable("DBPASSWORD") ?? "root";

      //services.AddDbContextPool<NewAgeDbContext>(options => options.UseMySql(Configuration.GetConnectionString("AuthDbConnection")));

      services.AddDbContextPool<NewAgeDbContext>(options => options.UseMySql($"Server={ server }; Uid=root; Pwd={ password }; Port={ port }; Database=NewAgeSolution"));

      services.AddControllersWithViews(options =>
      {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        options.Filters.Add(new AuthorizeFilter(policy));
      })
        .AddNewtonsoftJson();

      services.AddIdentity<Employee, IdentityRole>()
        .AddEntityFrameworkStores<NewAgeDbContext>()
        .AddDefaultTokenProviders();

      services.ConfigureApplicationCookie(options =>
      {
        options.AccessDeniedPath = new PathString("/AccessDenied");
        options.LoginPath = new PathString("/Account/Login");
      });

      services.AddScoped<IEmployee, SqlEmployee>();
      services.AddScoped<IRackspace, RackspaceEmailSender>();
      services.AddScoped<IChannelAdvisor, ChannelAdvisor>();
      services.AddScoped<ISkuVault, SkuVault>();
      services.AddScoped<IFileReader, FileReader>();

      services.AddTransient<IEmailSender, EmailSender>();

      services.AddSession();

      services.AddAuthorization(options =>
      {
        options.AddPolicy("Admin", policy => policy.RequireClaim(ClaimTypeEnum.Admin.ToString(), "true"));
      });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, NewAgeDbContext context)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      //app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseSession();

      //context.Database.Migrate();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
      });
    }
  }
}

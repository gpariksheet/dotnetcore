
#region before mvc
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;

//namespace EmployeeManagement
//{
//    public class Startup
//    {
//        // This method gets called by the runtime. Use this method to add services to the container.
//        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
//        private IConfiguration _configuration;
//        public Startup(IConfiguration configuration)
//        {
//            this._configuration = configuration;
//        }
//        public void ConfigureServices(IServiceCollection services)
//        {
//            services.AddMvc();
//        }

//        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
//        #region first 15 parts of tutorial
//        //public void Configure(IApplicationBuilder app, IHostingEnvironment env)
//        //{
//        //    if (env.IsDevelopment())
//        //    {
//        //        app.UseDeveloperExceptionPage();
//        //    }

//        //    //app.Run(async (context) =>
//        //    //{
//        //    //    //await context.Response.WriteAsync("Hello World!");
//        //    //    await context.Response.WriteAsync(System.Diagnostics.Process.GetCurrentProcess().ProcessName+"\n");
//        //    //    await context.Response.WriteAsync(_configuration["MyKey"]);
//        //    //});
//        //    app.Use(async (context, next) =>
//        //    {
//        //        await context.Response.WriteAsync("Hello from 1st Middleware");
//        //        await next();
//        //    });

//        //    app.Run(async (context) =>
//        //    {
//        //        await context.Response.WriteAsync("Hello from 2nd Middleware");
//        //    });
//        //}
//        #endregion
//        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
//        {
//            #region first 15 parts of tutorial
//            //DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
//            //defaultFilesOptions.DefaultFileNames.Clear();
//            //defaultFilesOptions.DefaultFileNames.Add("index.html");

//            //app.UseDefaultFiles();
//            //app.UseStaticFiles();
//            //if (env.IsDevelopment())
//            //{
//            //    app.UseDeveloperExceptionPage();
//            //}

//            //app.Run(async (context) =>
//            //{
//            //    await context.Response.WriteAsync("Hosting Environment: " + env.EnvironmentName);
//            //});
//            #endregion

//            if (env.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//            }

//            app.Run(async (context) =>
//            {
//                await context.Response.WriteAsync("Hosting Environment: " + env.EnvironmentName);
//            });
//        }
//    }
//}
#endregion



using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using EmployeeManagement.Security;
using Microsoft.CodeAnalysis.Options;

namespace EmployeeManagement
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        private IConfiguration _configuration;
        public Startup(IConfiguration configuration)
        {
            this._configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddDbContextPool<AppDbContext>(
                options => options.UseSqlServer(_configuration.GetConnectionString("EmployeeDBConnection"))
                );
            services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequiredLength = 10;
                    options.Password.RequiredUniqueChars = 3;
                    options.Password.RequireNonAlphanumeric = false;

                    options.SignIn.RequireConfirmedEmail = true;

                    options.Tokens.EmailConfirmationTokenProvider = "CustomEmailConfirmation";

                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddDefaultTokenProviders()
                .AddTokenProvider<CustomEmailConfirmationTokenProvider<ApplicationUser>>("CustomEmailConfirmation")
                .AddEntityFrameworkStores<AppDbContext>();

            // Set token life span to 5 hours
            services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(5));
            //set confirm email token life span to 3 days
            services.Configure<CustomEmailConfirmationTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromDays(3));
            //Identity options can be configured either using services.configure() or services.AddIdentity()
            //services
            //    .Configure<IdentityOptions>(options =>
            //    {
            //        options.Password.RequiredLength = 10;
            //        options.Password.RequiredUniqueChars = 3;
            //        options.Password.RequireNonAlphanumeric = false;
            //    });

            services
                .AddMvc(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                });
            //.AddXmlSerializerFormatters();  //commented because we're not using XML in our app
            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();

            services
                .ConfigureApplicationCookie(options =>
                {
                    options.AccessDeniedPath = new PathString("/administration/accessdenied");
                });

            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy("DeleteRolePolicy", policy => policy.RequireClaim("Delete Role", "true"));
                    options.AddPolicy("AdminRolePolicy", policy => policy.RequireRole("admin"));
                    options.AddPolicy("EditRolePolicy", policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));
                });
            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();
            services.AddSingleton<DataProtectionPurposeStrings>();
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("EditRolePolicy", policy => policy.RequireClaim("Edit Role", "true"));
            //});
            //services.AddAuthorization(options =>
            //     {
            //         options.AddPolicy("EditRolePolicy",
            //             policy => policy.RequireAssertion(context =>
            //                 context.User.IsInRole("admin") &&
            //                 context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
            //                 context.User.IsInRole("superadmin")
            //             ));
            //     });

            #region external authentication (Google and Facebook)
            services
                .AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = "912600428612-2guvaqiqju3qbt0cf3ju4mok0p787hvc.apps.googleusercontent.com";
                    options.ClientSecret = "LLYgec53nFEErABJu_kUWnrW";
                })
                .AddFacebook(options =>
                {
                    options.AppId = "2683829828520368";
                    options.AppSecret = "0a8554c9e480da0b617b3cff451baf2d";
                });
            #endregion

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=home}/{action=index}/{id?}");
            });
        }
    }
}

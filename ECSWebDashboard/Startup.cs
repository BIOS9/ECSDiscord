using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ECSWebDashboard.Util;
//using ECSDiscord.Util;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
//using Serilog;

namespace ECSWebDashboard
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
            services.AddDiscord();
            services.AddControllers();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
#if DEBUG
                options.Authority = "https://localhost:5001";
#else
                options.Authority = "https://ecsauth.nightfish.co";
#endif
                options.Audience = "ecsdiscord";
#if DEBUG
                options.RequireHttpsMetadata = false;
#endif
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("member", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("discord:id");
                    policy.RequireAssertion(DiscordServiceProvider.IsMember);
                });
                options.AddPolicy("admin", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("discord:id");
                    policy.RequireAssertion(DiscordServiceProvider.IsAdmin);
                });
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => builder
#if DEBUG
                    .WithOrigins("http://localhost:3000")
#endif
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseOptions();
            app.UseCors();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseFileServer();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("/index.html");
            });
            app.UseDiscord(new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(ECSDiscord.ECSDiscord.LogFileName, rollingInterval: ECSDiscord.ECSDiscord.LogInterval));
        }
    }
}

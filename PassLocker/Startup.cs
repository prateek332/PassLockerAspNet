using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using PassLocker.Services.GoogleLogin;
using PassLocker.Services.Protector;
using PassLocker.Services.Token;
using PassLocker.Services.UserDatabase;
using PassLockerDatabase;

namespace PassLocker
{
    public class Startup
    {
        // policy name for react app
        private const string MyAllowSpecificOrigins = "PassLockerFE";
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<PassLockerDbContext>(options =>
                options.UseSqlServer(
                    Environment.GetEnvironmentVariable("DB_SQL_SERVER_EXPRESS") ?? 
                    BackupConnectionString()));

            // Register custom Password Hashing Service
            services.AddScoped<IProtector, Protector>();
            
            // Register custom Google Authentication service
            services.AddScoped<IGoogleLogin, GoogleLogin>();

            // Register custom UserDatabase service
            services.AddScoped<IUserDatabase, UserDatabase>();
            
            // Register custom token service
            services.AddScoped<ITokenService, TokenService>();

            services.AddControllers()
                .AddXmlDataContractSerializerFormatters()
                .AddXmlSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder.WithOrigins(Configuration["ReactConfig:localhostUrl1"],
                                Configuration["ReactConfig:localhostUrl2"])
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "PassLocker", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PassLocker v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseCors(MyAllowSpecificOrigins);

            // app.UseAuthorization();
            // app.UseAuthentication();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                //endpoints.MapFallbackToFile("index.html");
            });
        }
        
        /// <summary>
        /// This method is for development and testing purpose only.
        /// Remove this method in production
        /// </summary>
        /// <returns>Sql connection string for the current OS.</returns>
        private string BackupConnectionString()
        {
            if (Environment.OSVersion.ToString().Contains("Unix", StringComparison.OrdinalIgnoreCase))
            {
                return "Data Source=acc;Initial Catalog=PassLocker;User id=sa;Password=Prateek332@#;";
            }

            return @"Server=PRATEEKPC\SQLEXPRESS;Database=PassLocker;Trusted_Connection=True;";
        }
    }
}
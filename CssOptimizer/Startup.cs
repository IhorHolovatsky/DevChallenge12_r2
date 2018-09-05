using System.Text;
using System.Threading.Tasks;
using CssOptimizer.Api.Filters;
using CssOptimizer.Domain.Configuration;
using CssOptimizer.Domain.Constants;
using CssOptimizer.Services.ChromeServices;
using CssOptimizer.Services.Implementations;
using CssOptimizer.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace CssOptimizer.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public string ApiName { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ApiName = "Css optimizer Api";
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IBrowserOptimizeCssService, BrowserOptimizeCssService>();
            services.AddScoped<ICustomOptimizeCssService, CustomOptimizeCssService>();

            services.AddMvc(opt => opt.Filters.Add<GlobalExceptionFilter>());

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = ApiName,
                        Version = "v1"
                    });

                // UseFullTypeNameInSchemaIds replacement for .NET Core
                c.CustomSchemaIds(x => x.FullName);
            });

            //just use this to be sure that AngleSharp lib will be loaded
            var initAngleSharp = AngleSharp.Configuration.Default.GetType();

            //Register popular encoding providers (to be able parse response from http client)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            //Register options
            var chromeSessionPoolConfig = Configuration.GetSection(ConfigurationConstants.CHROME_SESSION_POOL_CONFIGURATION_SECTION);
            services.Configure<CacheConfiguration>(Configuration.GetSection(ConfigurationConstants.CACHE_CONFIGURATION_SECTION));
            services.Configure<ChromeSessionPoolConfiguration>(chromeSessionPoolConfig);

            //Don't wait until chrome sessions pool will be initialize.
            //In production probably will be better to wait.
            var chromeSessionPoolConfigObject = chromeSessionPoolConfig.Get<ChromeSessionPoolConfiguration>();
            if (chromeSessionPoolConfigObject.IsPreInitializeChromeSessionPool)
            {
                if (chromeSessionPoolConfigObject.WaitForInitializing)
                {
                    Task.WaitAll(ChromeSessionPool.InitPool(chromeSessionPoolConfigObject));
                }
                else
                {
                    ChromeSessionPool.InitPool(chromeSessionPoolConfigObject);
                }
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("../swagger/v1/swagger.json", ApiName);
                opt.DisplayRequestDuration();
            });

            app.UseMvc();

            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        private void OnShutdown()
        {
            ChromeSessionPool.Dispose();
        }
    }
}

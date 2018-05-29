using CssOptimizer.Api.Filters;
using CssOptimizer.Services.ChromeServices;
using CssOptimizer.Services.Implementations;
using CssOptimizer.Services.Interfaces;
using MasterDevs.ChromeDevTools;
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

            services.AddMvc(opt => opt.Filters.Add<GlobalExceptionFilter>());
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Title = ApiName,
                        Version = "v1"
                    });
            });

            ChromeSessionPool.InitPool();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
        }
    }
}

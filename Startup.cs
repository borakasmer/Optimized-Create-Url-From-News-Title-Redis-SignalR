using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace codernews
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
            /*  services.AddSingleton<IFileProvider>(
                new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))); */
                    
            services.AddMvc();
            services.AddSignalR();
            services.AddDistributedRedisCache(option=>{
                    option.Configuration="127.0.01:6379";
                    option.InstanceName="master";                    
            });
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {                                           
                routes.MapRoute(
                    name: "admin",
                    template: "Admin",
                    defaults: new { controller = "News", action = "Admin" });

                  routes.MapRoute(
                    name: "adminsave",
                    template: "{title}/SaveNews",
                    defaults: new { controller = "News", action = "SaveNews" });

                routes.MapRoute(
                    name: "blog",
                    template: "{title}/{id}",
                    defaults: new { controller = "News", action = "Detail" });

                routes.MapRoute(
                    name: "default",
                    template: "{controller=News}/{action=Index}/{id?}");
            });
            
           app.UseSignalR(routes =>
           {
               routes.MapHub<NewsPush>("newspush");
           });
        }       
    }
}

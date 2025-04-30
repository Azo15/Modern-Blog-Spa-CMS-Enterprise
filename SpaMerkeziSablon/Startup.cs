using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using DataAccesLayer.EntityFramework;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SpaMerkeziSablon
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
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<AppUser, AppRole>()
                .AddEntityFrameworkStores<AppDbContext>();

            services.AddScoped<IBlogDal, EFBlogDal>();
            services.AddScoped<IBlogService, BlogManager>();

            services.AddScoped<IAboutDal, EFAboutDal>();
            services.AddScoped<IAboutService, AboutManager>();

            services.AddScoped<IContactDal, EFContactDal>();
            services.AddScoped<IContactService, ContactManager>();

            services.AddScoped<ICatagoryDal, EFCatagoryDal>();
            services.AddScoped<ICatagoryService, CatagoryManager>();

            services.AddScoped<IGalaryDal, EFGalaryDal>();
            services.AddScoped<IGalaryService, GalaryManager>();

            services.AddScoped<IHomeDal, EFHomeDal>();
            services.AddScoped<IHomeService, HomeManager>();

            services.AddScoped<ILinkDal, EFLinkDal>();
            services.AddScoped<ILinkService, LinkManager>();

            services.AddScoped<IMessageDal, EFMessageDal>();
            services.AddScoped<IMessageService, MessageManager>();

            services.AddScoped<IServiceDal, EFServiceDal>();
            services.AddScoped<IServiceService, ServiceManager>();

            services.AddControllersWithViews(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            });

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Error/AccessDenied";
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Default}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "404-CatchAll",
                    pattern: "{*url}",
                    defaults: new { controller = "Error", action = "PageNotFound" });

            });


        }
    }
}

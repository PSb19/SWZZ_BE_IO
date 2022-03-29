using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Threading.Tasks;

using SWZZ_Backend.Models;
using SWZZ_Backend.Data;
using SWZZ_Backend.Authorization;


namespace SWZZ_Backend
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
            services.AddControllers();
            // Use for connection with local database
            //services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer("Server=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;"), ServiceLifetime.Transient, ServiceLifetime.Transient);

            // Use for connection with Azure database
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlServer("Server=tcp:dev-swzz.database.windows.net,1433;Initial Catalog=devSwzzMain;Persist Security Info=False;User ID=swzz-main-sa;Password=f5kbNqBV3Uu6cFfRly;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"), ServiceLifetime.Transient, ServiceLifetime.Transient);
            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SWZZ_Backend", Version = "v1" });
            });

            services.AddDistributedMemoryCache();
            
            services.AddSession(options => 
            {
                options.IdleTimeout = System.TimeSpan.FromHours(1);
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.IsEssential = true;
            });

            services.ConfigureApplicationCookie(options => 
            {
                options.LoginPath = "/login";
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = 401;    
                    return Task.CompletedTask;
                };
            });

            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

            services.AddSingleton<IGroupAuthorizationRequirement, GroupAuthorizationRequirement>();
            services.AddTransient<IGroupAuthorizationHandler, GroupAuthorizationHandler>();
            services.AddSingleton<ITaskAuthorizationRequirement, TaskAuthorizationRequirement>();
            services.AddTransient<ITaskAuthorizationHandler, TaskAuthorizationHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SWZZ_Backend v1"));
            
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}

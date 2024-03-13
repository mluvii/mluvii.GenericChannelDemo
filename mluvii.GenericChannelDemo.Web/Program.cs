using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using mluvii.GenericChannelDemo.Web.Hubs;

namespace mluvii.GenericChannelDemo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}

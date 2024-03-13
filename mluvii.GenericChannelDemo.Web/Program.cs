using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using mluvii.GenericChannelDemo.Web.Hubs;
using mluvii.GenericChannelDemo.Web.Services;
using mluvii.GenericChannelDemo.Web.Services.Impl;

namespace mluvii.GenericChannelDemo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR();
            builder.Services.AddSingleton(typeof(IUserIdProvider), typeof(ChatHub.UserIdProvider));
            builder.Services.AddControllersWithViews();

            builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

            builder.Services.AddSingleton<IChatService, ChatService>();

            var app = builder.Build();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}

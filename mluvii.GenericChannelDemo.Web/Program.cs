using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using mluvii.GenericChannelDemo.Web.Hubs;
using mluvii.GenericChannelDemo.Web.Serialization;
using mluvii.GenericChannelDemo.Web.Services;
using mluvii.GenericChannelDemo.Web.Services.Impl;
using Newtonsoft.Json;

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

            builder.Services.AddMvc().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
                options.SerializerSettings.Converters = new List<JsonConverter>()
                {
                    new SafeStringEnumConverter()
                };
            });

            builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IChatService, ChatService>();

            builder.Services.Configure<GenericChannelOptions>(builder.Configuration.GetSection("GenericChannel"));

            builder.Services.AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddBasic(options =>
                {
                    options.Realm = "genericchanneldemo";
                    options.AllowInsecureProtocol = true;
                    options.Events = new BasicAuthenticationEvents
                    {
                        OnValidateCredentials = context =>
                        {
                            var config = context.HttpContext.RequestServices
                                .GetService<IOptions<GenericChannelOptions>>();

                            if (config.Value.ApiUserName != context.Username ||
                                config.Value.ApiPassword != context.Password)
                            {
                                return Task.CompletedTask;
                            }

                            var claims = new[]
                            {
                                new Claim(ClaimTypes.Name,
                                    context.Username,
                                    ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer),
                                new Claim(ClaimTypes.Role,
                                    "GenericChannel",
                                    ClaimValueTypes.String,
                                    context.Options.ClaimsIssuer)
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                            context.Success();

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "api",
                pattern: "{area:exists}/{controller}/{action}");
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}

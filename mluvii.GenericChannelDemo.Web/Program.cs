using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using mluvii.GenericChannelDemo.Web.Hubs;
using mluvii.GenericChannelDemo.Web.Serialization;
using mluvii.GenericChannelDemo.Web.Services;
using mluvii.GenericChannelDemo.Web.Services.Impl;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace mluvii.GenericChannelDemo.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSignalR().AddNewtonsoftJsonProtocol();
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
            builder.Services.AddSingleton<IChatReceivedMessageNotifier, ChatHub.ReceivedMessageNotifier>();

            builder.Services.Configure<GenericChannelOptions>(builder.Configuration.GetSection("GenericChannel"));

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.RequireHeaderSymmetry = false;
                options.KnownNetworks.Add(new IPNetwork(IPAddress.Any, 0));
                options.KnownNetworks.Add(new IPNetwork(IPAddress.IPv6Any, 0));
                options.ForwardLimit = 1;
            });

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

            builder.Services.AddOptions<SwaggerGenOptions>()
                .Configure(options =>
                {
                    options.DocInclusionPredicate((docName, apiDesc) => true);
#pragma warning disable 618
                    options.DescribeAllEnumsAsStrings();
#pragma warning restore 618
                    options.IgnoreObsoleteProperties();

                    options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
                    {
                        Description = "Basic auth added to authorization header",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Scheme = "basic",
                        Type = SecuritySchemeType.Http
                    });

                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Basic" }
                            },
                            new List<string>()
                        }
                    });

                    options.SwaggerDoc("api", new OpenApiInfo
                    {
                        Title = "GenericChannelDemo API",
                        Version = "latest"
                    });
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSwaggerGenNewtonsoftSupport();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseForwardedHeaders();

            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = true;
                c.RouteTemplate = "{documentName}/swagger.json";
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    if (httpReq.Headers.TryGetValue("X-Forwarded-Prefix", out var value) && value.Count > 0)
                    {
                        swaggerDoc.Servers = new List<OpenApiServer> { new OpenApiServer { Url = value } };
                    }
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "api";
                c.DocumentTitle = "GenericChannelDemo API";
                c.SwaggerEndpoint("swagger.json", "GenericChannelDemo API");
            });

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }
    }
}

using AonFreelancing.Configs;
using AonFreelancing.Contexts;
using AonFreelancing.Jobs;
using AonFreelancing.Models;
using AonFreelancing.Models.Documents;
using AonFreelancing.Services;
using AonFreelancing.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using ZainCash.Net.Extensions;
using ZainCash.Net.Services;

namespace AonFreelancing.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
                options.SuppressAsyncSuffixInActionNames = false
            )
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            );

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            return services;
        }

        public static IServiceCollection AddCustomServices(this IServiceCollection services)
        {
            services.AddSingleton<OtpManager>();
            services.AddSingleton<JwtService>();
            services.AddSingleton<FileStorageService>();
            services.AddSingleton<InMemorySignalRUserConnectionService>();

            // Scoped services
            services.AddScoped<PushNotificationService>();
            services.AddScoped<OTPService>();
            services.AddScoped<TempUserService>();
            services.AddScoped<UserService>();
            services.AddScoped<RoleService>();
            services.AddScoped<AuthService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<ProjectLikeService>();
            services.AddScoped<ProjectService>();
            services.AddScoped<RatingService>();
            services.AddScoped<TaskService>();
            services.AddScoped<SkillsService>();
            services.AddScoped<BidService>();
            services.AddScoped<FreelancerService>();
            services.AddScoped<ActivitiesService>();
            services.AddScoped<CommentService>();
            services.AddScoped<ProfileService>();
            services.AddScoped<SubscriptionsService>();
            services.AddScoped<ElasticService<UserDocument>>();
            services.AddScoped<ElasticService<Project>>();
            services.AddScoped<WebRtcSignalingService>();
            services.AddScoped<ZainCashService>();

            // Additional services
            services.AddSignalR();
            services.AddEndpointsApiExplorer();
            
            return services;
        }

        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration conf)
        {
            services.AddDbContext<MainAppContext>(options =>
                options.UseSqlServer(conf.GetConnectionString("Default")));
            services.AddIdentity<User, ApplicationRole>()
                .AddEntityFrameworkStores<MainAppContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        public static IServiceCollection AddIntegrationConfigServices(this IServiceCollection services, IConfiguration conf)
        {
            services.AddZainCashConfig("ZainCash", conf);

            services.AddHostedService<ElsSetupJob>();
            services.Configure<ElasticSettings>(conf.GetSection("ElasticSettings"));

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration conf)
        {
            var jwtSettings = conf.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }

        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
            return services;
        }

        public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration conf)
        {
            int preFlightMaxAge = int.Parse(conf.GetSection("Cors")["PreFlightMaxAge"] ?? "10");

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .SetPreflightMaxAge(TimeSpan.FromMinutes(preFlightMaxAge));
                });
            });

            return services;
        }
    }
}

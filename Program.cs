using System.Net;
using AonFreelancing.Contexts;
using AonFreelancing.Enums;
using AonFreelancing.Hubs;
using AonFreelancing.Middlewares;
using AonFreelancing.Models;
using AonFreelancing.Services;
using AonFreelancing.Utilities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using AonFreelancing.Configs;
using AonFreelancing.Jobs;
using AonFreelancing.Models.Documents;
using ZainCash.Net.Extensions;
using ZainCash.Net.Services;
using AonFreelancing.Extensions;

namespace AonFreelancing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.ConfigureKestrel();
            builder.Services.AddCustomControllers();
            builder.Services.AddCustomServices();
            builder.Services.AddDatabaseServices(builder.Configuration);
            builder.Services.AddIntegrationConfigServices(builder.Configuration);
            builder.Services.AddCustomAuthentication(builder.Configuration);
            builder.Services.AddCustomSwagger();
            builder.Services.AddCustomCors(builder.Configuration);

            var app = builder.Build();

            app.ConfigurePipeline();
            app.SeedRoles();

            app.Run();
        }
    }
}

using E_Commerce.Application.Extensions;
using E_Commerce.Infrastructure.Extensions;
using E_Commerce_API.BackgroundServices;
using E_Commerce_API.Extensions;
using E_Commerce_API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;


namespace E_Commerce_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddOpenApi( options =>
            {
                                
            }
                );
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddApplication(builder.Configuration);

            builder.Services.AddOptions<OrderExpirationCancellationOptions>()
                .Bind(builder.Configuration.GetSection("OrderExpirationCancellation"))
                .Validate(o => o.IntervalSeconds > 0, "OrderExpirationCancellation:IntervalSeconds must be > 0.");

            builder.Services.AddHostedService<CancelExpiredPendingOrdersBackgroundService>();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var issuer = builder.Configuration["Jwt:Issuer"];
                    var audience = builder.Configuration["Jwt:Audience"];
                    var signingKey = builder.Configuration["Jwt:SigningKey"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            builder.Services.AddAuthorization();

            var app = builder.Build();            
            //app.EnsureDatabaseAndSeedAsync().GetAwaiter().GetResult();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi(); // exposes /openapi/v1.json

                
                app.MapScalarApiReference(options =>
                {
                    options.Title = "E-Commerce Project";
                    options.Theme = ScalarTheme.BluePlanet;
                });
            }


            app.UseMiddleware<HandleExceptionMiddleware>();
            app.UseHttpsRedirection();


            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}

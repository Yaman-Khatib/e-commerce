
using E_Commerce.Application.Extensions;
using E_Commerce_API.Extensions;
using E_Commerce_API.Middleware;
using E_Commerce.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using System.Text;
using E_Commerce_API.BackgroundServices;

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

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                const string SecuritySchemeId = "BearerAuth";

                options.AddSecurityDefinition(SecuritySchemeId, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header using the Bearer scheme"
                });
                
                options.AddSecurityRequirement(doc =>
                {
                    var schemeRef = new OpenApiSecuritySchemeReference(SecuritySchemeId);
                    return new OpenApiSecurityRequirement
                  {
                      { schemeRef, new List<string>() }
                        };
                     });
            });

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
            app.EnsureDatabaseAndSeedAsync().GetAwaiter().GetResult();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
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

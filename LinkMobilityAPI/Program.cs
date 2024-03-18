using LinkMobilityAPI.Database;
using LinkMobilityAPI.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi la configurazione del file appsettings.json
builder.Configuration.AddJsonFile("appsettings.json");

// Configure the JWT Authentication Service
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", jwtOptions =>
{
    jwtOptions.TokenValidationParameters = new TokenValidationParameters()
    {
        // The SigningKey is defined in the TokenController class
        IssuerSigningKey = Utils.SIGNING_KEY,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// Configurazione delle impostazioni del database
builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));

// Aggiungi il client MongoDB
builder.Services.AddSingleton<IMongoClient>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("MongoDB");
    return new MongoClient(connectionString);
});

// Aggiungi il database
builder.Services.AddScoped(provider =>
{
    var client = provider.GetRequiredService<IMongoClient>();
    var settings = provider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    return client.GetDatabase(settings.DatabaseName);
});

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "MyCorsPolicy",
                      policy =>
                      {
                          policy.WithOrigins("http://example.com", "http://localhost:4200", "http://localhost:1966", "https://localhost:1966")
                          .AllowAnyMethod()
                          .AllowCredentials()
                          .AllowAnyHeader();
                      });
});

var app = builder.Build();

// Configure CORS
app.UseCors("MyCorsPolicy");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

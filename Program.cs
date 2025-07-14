using MySql.Data.MySqlClient;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register QuestPDF license type before running the app
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Enables cookie/token sharing
    });
});


// Register MySQL connection as a service
string connectionString = "Server=oric-cust-oric-cust.e.aivencloud.com;Port=22764;Database=defaultdb;Uid=avnadmin;Pwd=AVNS_NtjyNqiW-y-uuHrq2K9;SslMode=Required;";
// string connectionString = "Server=localhost;Database=ORIC;User=root;Password=;";
builder.Services.AddTransient<MySqlConnection>(_ => new MySqlConnection(connectionString));

// ? JWT Configuration (New)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings.GetValue<string>("Key");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // ? Set true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Ensure database and table exist
InitializeDatabase(connectionString);

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowSpecificOrigin");

// ? Use Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

// Helper method to initialize the database
void InitializeDatabase(string connStr)
{
    using var connection = new MySqlConnection("Server=localhost;User=root;Password=;");
    try
    {
        connection.Open();

        using var createDbCommand = new MySqlCommand("CREATE DATABASE IF NOT EXISTS ORIC", connection);
        createDbCommand.ExecuteNonQuery();

        using var useDbCommand = new MySqlCommand("USE ORIC", connection);
        useDbCommand.ExecuteNonQuery();

        using var createTableCommand = new MySqlCommand(@"
            CREATE TABLE IF NOT EXISTS user (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(255) NOT NULL,
                email VARCHAR(255) NOT NULL,
                password VARCHAR(255) NOT NULL,
                number VARCHAR(20) NOT NULL,
                role VARCHAR(50) NOT NULL,
                department VARCHAR(255) NOT NULL,
                isActive BOOLEAN NOT NULL DEFAULT TRUE,
                last_active DATETIME NULL
            )", connection);
        createTableCommand.ExecuteNonQuery();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error initializing database: {ex.Message}");
    }
}

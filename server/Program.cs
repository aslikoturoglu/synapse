using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySql.EntityFrameworkCore.Extensions;
using Server.Data;
using Server.Services;
using Server.Services.AiFoundry;

// Community license (free for individuals/small teams under $1M annual revenue) — required
// by QuestPDF before generating any document, see PostService.GenerateNotePdfAsync.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ConnectionService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<RequestService>();
builder.Services.AddSingleton<EmailService>();

// FoundryAgentClient wraps a PersistentAgentsClient (thread-safe, reused like any other Azure
// SDK client), so it's a singleton; the higher-level AiFoundry services need AppDbContext
// (scoped) so they stay scoped like PostService.
builder.Services.AddSingleton<FoundryAgentClient>();
builder.Services.AddScoped<NoteCreationAiService>();
builder.Services.AddScoped<NoteMapAiService>();
builder.Services.AddScoped<NoteChatAiService>();
builder.Services.AddScoped<AdminChatAiService>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

const string ClientCorsPolicy = "ClientCorsPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No UseHttpsRedirection: this is a JSON API for a separate-origin SPA whose ApiBaseUrl
// is plain HTTP (wwwroot/appsettings.json) — redirecting to HTTPS here just breaks that
// contract (and the dev cert isn't trusted anyway) without buying any real security in local dev.

app.UseCors(ClientCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AdminSeeder.EnsureAdminAsync(db);
}

app.Run();

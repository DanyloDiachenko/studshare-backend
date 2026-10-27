using System.Text;
using DotNetEnv;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudShare.Api.Data;
using StudShare.Api.Endpoints;
using StudShare.Api.Services;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var baseConnection = builder.Configuration.GetConnectionString("DefaultConnection")!;
var connectionString = baseConnection.Contains("Password=")
    ? baseConnection
    : baseConnection + $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HistoryService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<FileService>();
builder.Services.AddScoped<FolderService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<CurrentUserService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<EmailVerificationService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!)),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new()
        {
            Title = "StudShare API",
            Version = "v1",
            Description = "API for the educational material sharing platform. " +
                          "Most endpoints require a JWT token in the `Authorization: Bearer <token>` header.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "StudShare API v1"));
}

if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapGet("/", () => Results.Ok(new { message = "StudShare API is running" }))
   .WithName("Root");

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapGroupEndpoints();
app.MapFilesEndpoints();
app.MapMembersEndpoints();
app.MapHistoryEndpoints();

app.Run();

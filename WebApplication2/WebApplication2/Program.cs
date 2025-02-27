using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using WebApplication2.Data;
using WebApplication2.Video;

var builder = WebApplication.CreateBuilder(args);

// ��������� ������������
var configuration = builder.Configuration;

// ��������� ��������
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

// ����������� ������� ��� ApplicationDbContext
builder.Services.AddScoped<Func<ApplicationDbContext>>(provider => () => provider.GetRequiredService<ApplicationDbContext>());

// ��������� JWT ��������������
var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"]);
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // ���������� true, ���� ����������� HTTPS
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ����������� ������������
builder.Services.AddControllers();

// ��������� Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ��������� VideoRecorderService
builder.Services.Configure<VideoRecorderSettings>(configuration.GetSection("VideoRecorderSettings"));
builder.Services.AddSingleton<VideoRecorderService>();
builder.Services.AddSingleton<CleanupSettingsService>(provider =>
        new CleanupSettingsService(Path.Combine(Directory.GetCurrentDirectory(), "cleanupSettings.json")));
builder.Services.AddHostedService<CleanupBackgroundService>();
// ����������� VideoRecorderHostedService
builder.Services.AddHostedService<VideoRecorderHostedService>();

// ��������� �����������
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

var app = builder.Build();

// ������ ������ ��� ���� ����� ��� ������ ����������
using (var scope = app.Services.CreateScope())
{
    var videoRecorderService = scope.ServiceProvider.GetRequiredService<VideoRecorderService>();
    await videoRecorderService.StartRecordingForAllCamerasAsync();
}

// ��������� ��������� ��������� HTTP-��������
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    });
}

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
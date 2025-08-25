using APITestApp.Models;
using APITestApp.Services;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

Env.Load();
builder.Configuration["Elasticsearch:Password"] = Environment.GetEnvironmentVariable("ELASTICSEARCH_PASSWORD");
builder.Configuration["EmailSettings:Password"] = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");


builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<LogService>();
builder.Services.AddScoped<EmailService>();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        c.RoutePrefix = "swagger"; 
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

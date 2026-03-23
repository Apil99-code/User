
using Payment.API;
using Payment.API.Repositories;
using Payment.API.Services;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddHttpClient();


builder.Services.AddOpenApi();
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<PaymentRepository>();
builder.Services.AddScoped<PaymentService>();


var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseExceptionHandler("/error");
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();


using Order.API;
using Order.API.Repositories;
using Order.API.Repository;
using Order.API.Services;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();

builder.Services.AddHttpClient();


builder.Services.AddOpenApi();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddSingleton<DapperContext>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductRepository>();


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

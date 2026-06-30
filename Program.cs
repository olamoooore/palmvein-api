using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();      //  Required for Swagger
builder.Services.AddSwaggerGen();                //  Required for Swagger

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();                            //  Swagger JSON endpoint
    app.UseSwaggerUI();                          //  Swagger UI page
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Azure.Communication.Email;
using Microsoft.AspNetCore.Identity;
using Presentation.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(x => new EmailClient(builder.Configuration["ACS:ConnectionStrings"]));
builder.Services.AddTransient<IVerificationService, VerificationService>();

var app = builder.Build();
app.MapOpenApi();
app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NAutowired;

namespace KScr.Server.Repo;

public class KScrRepoServer
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers().AddControllersAsServices();
        
        builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, NAutowiredControllerActivator>());
        
        builder.Services.AddScoped<Repository>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
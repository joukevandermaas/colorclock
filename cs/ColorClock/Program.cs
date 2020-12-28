using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using ColorClock.Services;
using Blazored.LocalStorage;

namespace ColorClock
{
  public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddBlazoredLocalStorage();

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddScoped<HueService>();

            var host = builder.Build();

            var hueService = host.Services.GetService<HueService>();
            await hueService.InitializeAsync();

            await host.RunAsync();
        }
    }
}

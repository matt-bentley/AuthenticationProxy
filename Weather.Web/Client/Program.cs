using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Weather.Web.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(serviceProvider =>
{
    var navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri($"{navigationManager.BaseUri}api/") };
});

await builder.Build().RunAsync();

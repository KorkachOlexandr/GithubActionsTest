using Blazored.LocalStorage;
using FileManager.Client;
using FileManager.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri("http://localhost:5001") 
});

// Register services
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<ApiService>();

await builder.Build().RunAsync();
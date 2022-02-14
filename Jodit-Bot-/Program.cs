using Jodit_Bot_;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;


public static class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseUrls("http://localhost:5001");
                webBuilder.UseStartup<Startup>();
            });
}
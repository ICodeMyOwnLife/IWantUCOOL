using CB.Net.SignalR.Server;
using IWantUServerInfrastructure;
using Microsoft.Owin;

[assembly: OwinStartup(typeof(Startup))]


namespace IWantUServerInfrastructure
{
    public class Startup: SignalRStartup { }
}
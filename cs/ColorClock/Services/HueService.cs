using Q42.HueApi;
using Q42.HueApi.Interfaces;

namespace ColorClock.Services
{
    public class HueService
    {
        public HueService()
        {
            var client = new LocalHueClient("192.168.178.10");
            client.Initialize("t2enGIRzvDs-RPwDwjHvKlTo3cubMbwCfg6sNvND");

            Client = client;
        }

        public IHueClient Client { get; }
    }
}
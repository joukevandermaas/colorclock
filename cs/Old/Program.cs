using Q42.HueApi;
using System;
using System.Threading.Tasks;

const string deskLight1 = "20";
const string deskLight2 = "21";

var client = new LocalHueClient("192.168.178.10");
client.Initialize("t2enGIRzvDs-RPwDwjHvKlTo3cubMbwCfg6sNvND");

await DoClock(TimeSpan.FromSeconds(60), deskLight1, deskLight2);

async Task DoClock(TimeSpan time, params string[] lightIds)
{
    var light = await client.GetLightAsync(lightIds[0]);
    var savedBrightness = light.State.Brightness;
    var savedHue = light.State.Hue;
    var savedSaturation = light.State.Saturation;

    const ushort red = 65186;
    const ushort yellow = 6790;
    const ushort green = 21001;

    var halfTime = time / 2;
    var tenPercentTime = time / 10;

    var command = new LightCommand()
    {
        On = true,
        Brightness = savedBrightness,
        Saturation = 0xff,
        TransitionTime = TimeSpan.FromSeconds(1)
    };

    command.Hue = green;
    await client.SendCommandAsync(command, lightIds);

    await Task.Delay(time / 2);

    command.Hue = yellow;
    await client.SendCommandAsync(command, lightIds);

    await Task.Delay(halfTime - tenPercentTime);

    command.Hue = red;
    await client.SendCommandAsync(command, lightIds);

    await Task.Delay(tenPercentTime);

    command.Alert = Alert.Multiple;
    await client.SendCommandAsync(command, lightIds);
    
    // restore to normal when flashing is done
    await Task.Delay(TimeSpan.FromSeconds(10));

    command.Hue = savedHue;
    command.Saturation = savedSaturation;
    command.Alert = Alert.None;
    await client.SendCommandAsync(command, lightIds);
}

async Task PrintLights()
{
    var lights = await client.GetLightsAsync();

    foreach (var light in lights)
    {
        Console.WriteLine("{0}: {1}", light.Id, light.Name);
    }
}

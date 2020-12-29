using Blazored.LocalStorage;
using Microsoft.JSInterop;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ColorClock.Services
{
    public class HueService
    {
        // todo use discovery, localstorage to store this instead of
        // hardcoding it (so it works on any hub) - this requires UI too
        const string username = "t2enGIRzvDs-RPwDwjHvKlTo3cubMbwCfg6sNvND";
        const string ip = "192.168.178.10";

        private readonly ILocalStorageService _localStorage;

        internal IHueClient Client { get; }

        /// <summary>
        /// Since username is hardcoded for now, this allows multiple
        /// devices to independently run timers
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// If true, the user has selected the lights to run the timers on
        /// </summary>
        public bool HasLightsConfigured { get; private set; }

        public HueService(ILocalStorageService localStorage)
        {
            var client = new LocalHueClient(ip);
            client.Initialize(username);

            Client = client;
            _localStorage = localStorage;
        }

        public async Task InitializeAsync()
        {
            // see if we already have a device id, if so we can
            // reuse a lot of the config which will speed up the
            // initialization
            var id = await _localStorage.GetItemAsync<string>("deviceId");

            if (id == null)
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 10);
                await _localStorage.SetItemAsync<string>("deviceId", id);
            }

            DeviceId = id;

            // load the selected lights to run the timer on. the user must
            // select these before any timers can be run, so there may not
            // be any on the first run
            var selectedLightIds = await _localStorage.GetItemAsync<string[]>("selectedLights");
            if (selectedLightIds != null && selectedLightIds.Length > 0)
            {
                // we must make sure that the scenes are configured in the hub,
                // by the time we actually run timers we just assume they are there.
                // this is also done each time new lights are selected.
                await CreateScenesAsync(selectedLightIds);
            }
            else
            {
                HasLightsConfigured = false;
            }
        }

        public async Task CreateScenesAsync(string[] lightIds)
        {
            if (lightIds == null || lightIds.Length == 0)
            {
                // can happen when the users deselects all lights
                // on the settings screen.
                return;
            }

            var scenes = (await Client.GetScenesAsync())
               .Where(scene => scene.Name.Contains(DeviceId));

            var redScene = GetOrCreateScene(scenes, "red");
            var yellowScene = GetOrCreateScene(scenes, "yellow");
            var greenScene = GetOrCreateScene(scenes, "green");
            var alertScene = GetOrCreateScene(scenes, "alert");

            const ushort red = 65186;
            const ushort yellow = 6790;
            const ushort green = 21001;

            ConfigureColor(redScene, lightIds, red);
            ConfigureColor(yellowScene, lightIds, yellow);
            ConfigureColor(greenScene, lightIds, green);
            ConfigureColor(alertScene, lightIds, red, brightness: 0x00);

            await Task.WhenAll(
                SaveSceneAsync(redScene),
                SaveSceneAsync(yellowScene),
                SaveSceneAsync(greenScene),
                SaveSceneAsync(alertScene)
            );

            HasLightsConfigured = true;
        }

        private async Task SaveSceneAsync(Scene scene)
        {
            if (scene.Id == null)
            {
                await Client.CreateSceneAsync(scene);
            }
            else
            {
                await Client.UpdateSceneAsync(scene.Id, scene);
            }
        }

        private Scene GetScene(IEnumerable<Scene> scenes, string name) =>
            scenes.FirstOrDefault(s => s.Name.EndsWith($"-{name}"));


        private Scene GetOrCreateScene(IEnumerable<Scene> scenes, string name) =>
            GetScene(scenes, name) ??
            new Scene()
            {
                Name = $"{DeviceId}-{name}",
                TransitionTime = TimeSpan.FromSeconds(1),
                Recycle = true,
            };

        private static void ConfigureColor(Scene scene, string[] lights, ushort color, byte brightness = 0xff)
        {
            var state = new State
            {
                On = true,
                Hue = color,
                Saturation = 0xff,
                Brightness = brightness,
            };

            scene.Lights = lights;
            scene.LightStates = lights.ToDictionary(l => l, l => state);
        }

        public async Task<string> GetRunningTimerAsync()
        {
            var timers = (await Client.GetSchedulesAsync()).Where(s => s.Name.Contains(DeviceId));

            if (timers.Any())
            {
                // we use the description field to notate which
                // timer is running (e.g. '5m' for a five minute timer)
                // this is used to update the ui.
                return timers.First().Description;
            }

            return null;
        }

        public async Task CancelTimerAsync()
        {
            var timers = (await Client.GetSchedulesAsync()).Where(s => s.Name.Contains(DeviceId));

            // deleting timers is equivalent to canceling them
            await Task.WhenAll(timers.Select(timer => Client.DeleteScheduleAsync(timer.Id)));
        }

        public async Task StartTimerAsync(string context, TimeSpan duration)
        {
            // first cancel any other running timers, per user device
            // there should only ever be one timer running.
            // this doesn't prevent two people from running a timer on the
            // same lights at the same time (in that case weird stuff happens)
            await CancelTimerAsync();

            var halftime = duration / 2;
            var tenpercent = duration / 10;
            var flashPeriod = TimeSpan.FromSeconds(1);

            var scenes = (await Client.GetScenesAsync())
                .Where(scene => scene.Name.Contains(DeviceId));

            var redScene = GetScene(scenes, "red");
            var yellowScene = GetScene(scenes, "yellow");
            var greenScene = GetScene(scenes, "green");
            var alertScene = GetScene(scenes, "alert");

            // schedules automatically get cleaned up after they
            // execute (this is the default behavior)
            await Task.WhenAll(
                Client.RecallSceneAsync(greenScene.Id),
                CreateScheduleAsync(context, "halftime", halftime, yellowScene.Id),
                CreateScheduleAsync(context, "tenpercent", duration - tenpercent, redScene.Id),
                // flash the light on and off
                CreateScheduleAsync(context, "finish", duration, alertScene.Id),
                CreateScheduleAsync(context, "finish2", duration + flashPeriod, redScene.Id),
                CreateScheduleAsync(context, "finish3", duration + (flashPeriod * 2), alertScene.Id),
                CreateScheduleAsync(context, "finish4", duration + (flashPeriod * 3), redScene.Id),
                CreateScheduleAsync(context, "finish5", duration + (flashPeriod * 4), alertScene.Id),
                CreateScheduleAsync(context, "finish6", duration + (flashPeriod * 5), redScene.Id)
            );
        }

        public async Task CreateScheduleAsync(string context, string name, TimeSpan time, string sceneId)
        {
            await Client.CreateScheduleAsync(new Schedule
            {
                Name = $"{DeviceId}-{name}",
                LocalTime = new HueDateTime() { TimerTime = time },
                Description = context,
                Command = new InternalBridgeCommand
                {
                    Address = $"/api/{username}/groups/0/action",
                    Body = new SceneCommand(sceneId),
                    Method = HttpMethod.Put,
                }
            });
        }
    }
}
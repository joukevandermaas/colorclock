using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace ColorClock.Services
{
    public static class Helpers
    {
        public static async Task LogObject<T>(this IJSRuntime runtime, T obj)
        {
            await runtime.InvokeVoidAsync("log", JsonSerializer.Serialize(obj));
        }
    }
}

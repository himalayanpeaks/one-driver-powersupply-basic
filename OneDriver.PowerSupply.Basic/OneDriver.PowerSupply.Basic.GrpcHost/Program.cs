using Microsoft.Azure.Devices.Client;
using OneDriver.PowerSupply.Basic;
using OneDriver.PowerSupply.Basic.Products;
using OneDriver.PowerSupply.Basic.GrpcHost.Services;
using OneDriver.Framework.Libs.Validator;
using Newtonsoft.Json;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

// Read connection string from configuration
var deviceConnectionString = builder.Configuration["AzureIoT:DeviceConnectionString"];

if (string.IsNullOrEmpty(deviceConnectionString))
{
    Console.WriteLine("❌ ERROR: Azure IoT connection string not found in appsettings.json");
    Console.WriteLine("Please add 'AzureIoT:DeviceConnectionString' to appsettings.Development.json");
    return;
}

Console.WriteLine("✅ Connected to Azure IoT Hub");
Console.WriteLine($"   Device: {deviceConnectionString.Split(';')[1].Replace("DeviceId=", "")}");

// Register DeviceClient and Device as singletons
builder.Services.AddSingleton<DeviceClient>(provider =>
{
    var client = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
    client.OpenAsync().Wait();
    return client;
});

builder.Services.AddSingleton<Device>(provider =>
{
    var validator = new ComportValidator();
    var hal = new Kd3005p();
    return new Device("Korad", validator, hal);
});

// Start app
var app = builder.Build();

// Get services
var deviceClient = app.Services.GetRequiredService<DeviceClient>();
var device = app.Services.GetRequiredService<Device>();

// Connect to COM5
Console.WriteLine("🔌 Connecting to COM5...");
var connectResult = device.Connect("COM5");
if (connectResult == 0)
{
    Console.WriteLine("✅ Successfully connected to power supply on COM5");
}
else
{
    Console.WriteLine($"❌ Failed to connect to COM5 (error code: {connectResult})");
    Console.WriteLine("⚠️  Application will continue but device control may not work");
}
#region LISTENING
_ = Task.Run(async () =>
{
    Console.WriteLine("Listening for cloud-to-device messages...");

    while (true)
    {
        var message = await deviceClient.ReceiveAsync();
        if (message != null)
        {
            string payload = Encoding.UTF8.GetString(message.GetBytes());
            Console.WriteLine($"Received C2D message: {payload}");

            try
            {
                dynamic command = JsonConvert.DeserializeObject(payload);

                if (command.action == "setVoltage")
                {
                    int channel = command.channel;
                    double voltage = command.voltage;
                    Console.WriteLine($"Setting voltage on channel {channel} to {voltage}V");
                    device.SetVolts(channel, voltage);
                }
                else if (command.action == "setCurrent")
                {
                    int channel = command.channel;
                    double current = command.current;
                    Console.WriteLine($"Setting current on channel {channel} to {current}A");
                    device.SetAmps(channel, current);
                }
                else if (command.action == "allChannelsOn")
                {
                    Console.WriteLine("Turning all channels ON");
                    device.AllChannelsOn();
                }
                else if (command.action == "allChannelsOff")
                {
                    Console.WriteLine("Turning all channels OFF");
                    device.AllChannelsOff();
                }
                else
                {
                    Console.WriteLine("Unknown command action.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}");
            }

            await deviceClient.CompleteAsync(message);
        }

        await Task.Delay(1000); // Poll interval
    }
});
#endregion
app.MapGrpcService<PowerSupplyService>();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Power supply server is running.");

app.Run();

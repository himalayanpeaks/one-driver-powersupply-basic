# Quick Start - Azure IoT Hub Integration

## 🚀 Get Started in 5 Minutes

### 1. **Create Azure IoT Hub** (One-time setup)

```bash
# Install Azure CLI if you haven't: https://aka.ms/installazurecliwindows

# Login
az login

# Create resources
az group create --name rg-powersupply --location eastus

az iot hub create \
  --resource-group rg-powersupply \
  --name iot-powersupply-$(Get-Random) \
  --sku F1

# Note the IoT Hub name from output
```

### 2. **Register Your Device**

```bash
# Replace <YOUR_HUB_NAME> with your IoT Hub name
az iot hub device-identity create \
  --hub-name <YOUR_HUB_NAME> \
  --device-id powersupply-001

# Get connection string
az iot hub device-identity connection-string show \
  --hub-name <YOUR_HUB_NAME> \
  --device-id powersupply-001
```

**Copy the `connectionString` value!**

### 3. **Configure Application**

Update `appsettings.Development.json`:

```json
{
  "AzureIoT": {
    "DeviceConnectionString": "<PASTE_YOUR_CONNECTION_STRING_HERE>"
  }
}
```

### 4. **Run the Service**

```bash
cd OneDriver.PowerSupply.Basic.GrpcHost
dotnet run
```

Expected output:
```
Connecting to Azure IoT Hub...
✓ Connected to Azure IoT Hub
✓ Device initialized
Listening for cloud-to-device messages...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 5. **Test Cloud Commands**

**Open a new terminal** and send a command:

```bash
# Set voltage to 12V on channel 0
az iot device c2d-message send \
  --hub-name <YOUR_HUB_NAME> \
  --device-id powersupply-001 \
  --data '{"action":"setVoltage","channel":0,"voltage":12.0}'
```

Check your application logs - you should see:
```
Received C2D message: {"action":"setVoltage","channel":0,"voltage":12.0}
Setting voltage on channel 0 to 12V
```

### 6. **Monitor Telemetry** (Optional)

First, start streaming telemetry by calling the gRPC service:

```bash
# Using grpcurl (install: https://github.com/fullstorydev/grpcurl)
grpcurl -plaintext -d '{"channelNumber": 0}' localhost:5000 power.PowerSupply/StreamProcessData
```

Then monitor in Azure:

```bash
az iot hub monitor-events \
  --hub-name <YOUR_HUB_NAME> \
  --device-id powersupply-001
```

You should see telemetry data flowing:
```json
{
  "channel": 0,
  "voltage": 12.5,
  "current": 2.3,
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

## 🎉 That's It!

You now have:
- ✅ Power supply connected to Azure IoT Hub
- ✅ Remote control via Cloud-to-Device messages
- ✅ Real-time telemetry streaming
- ✅ gRPC API for local/remote access

## 📚 Next Steps

- **Deploy to Azure**: See [AZURE_SETUP.md](AZURE_SETUP.md) for Container Apps deployment
- **Add Monitoring**: Set up Azure Monitor and alerts
- **Build Dashboards**: Connect to Power BI or Grafana
- **Scale**: Use Device Provisioning Service for multiple devices

## 🔧 Available Commands

### Cloud-to-Device Commands

```json
// Set voltage
{"action":"setVoltage", "channel":0, "voltage":12.5}

// Set current
{"action":"setCurrent", "channel":0, "current":2.5}

// Turn all channels on
{"action":"allChannelsOn"}

// Turn all channels off
{"action":"allChannelsOff"}
```

### gRPC API Methods

- `OpenConnection(port)` - Connect to power supply
- `SetVolts(channel, value)` - Set voltage
- `SetAmps(channel, value)` - Set current  
- `AllChannelsOn()` - Enable all channels
- `AllChannelsOff()` - Disable all channels
- `StreamProcessData(channel)` - Stream telemetry

## ❓ Troubleshooting

**"Connection string not configured"**
- Check `appsettings.Development.json` has correct connection string

**"Device not found"**
- Verify device ID matches in connection string and Azure

**"No telemetry received"**
- Ensure `StreamProcessData` gRPC method is being called
- Check device is connected to physical hardware

## 💡 Tips

- Use **Azure IoT Explorer** GUI for easier testing: https://github.com/Azure/azure-iot-explorer
- Enable logging: Set `"Default": "Debug"` in appsettings
- Free tier has 8000 messages/day limit - sufficient for testing

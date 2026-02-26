# Azure IoT Hub Setup Guide

## Prerequisites
- Azure Subscription
- Azure CLI installed
- .NET 10 SDK

## Step 1: Create Azure IoT Hub

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-powersupply --location eastus

# Create IoT Hub (Free tier or Standard)
az iot hub create --resource-group rg-powersupply \
  --name iot-powersupply-hub \
  --sku F1 \
  --partition-count 2

# Or for production (Standard tier with more capacity)
az iot hub create --resource-group rg-powersupply \
  --name iot-powersupply-hub \
  --sku S1 \
  --partition-count 4
```

## Step 2: Register IoT Device

```bash
# Create a device identity
az iot hub device-identity create \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001

# Get the device connection string
az iot hub device-identity connection-string show \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --output table
```

**Copy the connection string** - it looks like:
```
HostName=iot-powersupply-hub.azure-devices.net;DeviceId=powersupply-device-001;SharedAccessKey=XXXXXXXX
```

## Step 3: Configure the Application

### Option A: Using appsettings.Development.json (Local Development)

Update `appsettings.Development.json`:
```json
{
  "AzureIoT": {
    "DeviceConnectionString": "HostName=iot-powersupply-hub.azure-devices.net;DeviceId=powersupply-device-001;SharedAccessKey=YOUR_KEY"
  }
}
```

### Option B: Using Environment Variables (Recommended for Production)

**Windows:**
```powershell
$env:AZURE_IOT_DEVICE_CONNECTION_STRING="HostName=iot-powersupply-hub.azure-devices.net;DeviceId=powersupply-device-001;SharedAccessKey=YOUR_KEY"
```

**Linux/Mac:**
```bash
export AZURE_IOT_DEVICE_CONNECTION_STRING="HostName=iot-powersupply-hub.azure-devices.net;DeviceId=powersupply-device-001;SharedAccessKey=YOUR_KEY"
```

### Option C: Using Azure Key Vault (Best for Production)

See the Azure Key Vault section below.

## Step 4: Run the Application

```bash
cd OneDriver.PowerSupply.Basic.GrpcHost
dotnet run
```

The application will:
- ✓ Connect to Azure IoT Hub
- ✓ Start gRPC server on port 5000 (HTTP) and 5001 (HTTPS)
- ✓ Listen for Cloud-to-Device (C2D) messages
- ✓ Stream telemetry when `StreamProcessData` is called

## Step 5: Test Cloud-to-Device Commands

### Using Azure CLI

```bash
# Send command to set voltage
az iot device c2d-message send \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --data '{"action":"setVoltage","channel":0,"voltage":12.5}'

# Send command to set current
az iot device c2d-message send \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --data '{"action":"setCurrent","channel":0,"current":2.5}'

# Turn all channels on
az iot device c2d-message send \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --data '{"action":"allChannelsOn"}'

# Turn all channels off
az iot device c2d-message send \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --data '{"action":"allChannelsOff"}'
```

### Using Azure IoT Explorer (GUI Tool)

1. Download [Azure IoT Explorer](https://github.com/Azure/azure-iot-explorer/releases)
2. Add your IoT Hub connection string
3. Select your device
4. Go to "Cloud-to-device message" tab
5. Send JSON commands

## Step 6: Monitor Telemetry

```bash
# Monitor device-to-cloud messages
az iot hub monitor-events \
  --hub-name iot-powersupply-hub \
  --device-id powersupply-device-001 \
  --output table
```

You should see telemetry like:
```json
{
  "channel": 0,
  "voltage": 12.5,
  "current": 2.3,
  "timestamp": "2024-01-15T10:30:45.123Z"
}
```

## Step 7: Deploy to Azure Container Apps (Optional)

### Build Docker Image

Create `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OneDriver.PowerSupply.Basic.GrpcHost/OneDriver.PowerSupply.Basic.GrpcHost.csproj", "OneDriver.PowerSupply.Basic.GrpcHost/"]
COPY ["OneDriver.PowerSupply.Basic/OneDriver.PowerSupply.Basic.csproj", "OneDriver.PowerSupply.Basic/"]
RUN dotnet restore "OneDriver.PowerSupply.Basic.GrpcHost/OneDriver.PowerSupply.Basic.GrpcHost.csproj"
COPY . .
WORKDIR "/src/OneDriver.PowerSupply.Basic.GrpcHost"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneDriver.PowerSupply.Basic.GrpcHost.dll"]
```

### Deploy to Azure Container Apps

```bash
# Create Azure Container Registry
az acr create --resource-group rg-powersupply \
  --name acrpowersupply \
  --sku Basic

# Build and push image
az acr build --registry acrpowersupply \
  --image powersupply-grpc:latest \
  --file Dockerfile .

# Create Container App Environment
az containerapp env create \
  --name env-powersupply \
  --resource-group rg-powersupply \
  --location eastus

# Deploy Container App
az containerapp create \
  --name app-powersupply-grpc \
  --resource-group rg-powersupply \
  --environment env-powersupply \
  --image acrpowersupply.azurecr.io/powersupply-grpc:latest \
  --target-port 80 \
  --ingress external \
  --env-vars AZURE_IOT_DEVICE_CONNECTION_STRING="secretref:iot-connection" \
  --secrets iot-connection="YOUR_CONNECTION_STRING"
```

## Using Azure Key Vault (Production Best Practice)

### 1. Create Key Vault

```bash
az keyvault create \
  --name kv-powersupply \
  --resource-group rg-powersupply \
  --location eastus
```

### 2. Store Connection String

```bash
az keyvault secret set \
  --vault-name kv-powersupply \
  --name IoTDeviceConnectionString \
  --value "HostName=iot-powersupply-hub.azure-devices.net;DeviceId=powersupply-device-001;SharedAccessKey=YOUR_KEY"
```

### 3. Update Application to Use Key Vault

Add package:
```bash
dotnet add package Azure.Identity
dotnet add package Azure.Security.KeyVault.Secrets
```

Update `Program.cs` to fetch from Key Vault (see code example below).

## Troubleshooting

### Connection Issues
- Verify connection string format
- Check firewall rules on IoT Hub
- Ensure device is registered

### No Telemetry Received
- Check if `StreamProcessData` gRPC method is being called
- Verify device is connected to physical power supply
- Check logs for errors

### C2D Messages Not Received
- Ensure the background task is running
- Check message format matches expected JSON
- Verify device is online in Azure Portal

## Security Best Practices

1. **Never commit connection strings** to Git
2. Use **Azure Key Vault** for production
3. Use **Managed Identity** when deployed to Azure
4. Rotate keys regularly
5. Use **Device Provisioning Service (DPS)** for fleet management

## Cost Estimation

- **IoT Hub Free Tier**: $0/month (8000 messages/day limit)
- **IoT Hub S1**: ~$25/month (400,000 messages/day)
- **Container Apps**: ~$15-30/month (depending on scale)
- **Key Vault**: ~$0.03/10,000 operations

## Next Steps

- [ ] Set up Azure Monitor for telemetry analytics
- [ ] Create Azure Stream Analytics for real-time processing
- [ ] Set up Power BI dashboards
- [ ] Implement Device Twin for device configuration
- [ ] Set up alerts and notifications

## Support

For issues, please open a GitHub issue or contact support.

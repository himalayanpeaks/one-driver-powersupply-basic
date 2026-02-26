FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["OneDriver.PowerSupply.Basic.GrpcHost/OneDriver.PowerSupply.Basic.GrpcHost.csproj", "OneDriver.PowerSupply.Basic.GrpcHost/"]
COPY ["OneDriver.PowerSupply.Basic/OneDriver.PowerSupply.Basic.csproj", "OneDriver.PowerSupply.Basic/"]
RUN dotnet restore "OneDriver.PowerSupply.Basic.GrpcHost/OneDriver.PowerSupply.Basic.GrpcHost.csproj"
COPY . .
WORKDIR "/src/OneDriver.PowerSupply.Basic.GrpcHost"
RUN dotnet build "OneDriver.PowerSupply.Basic.GrpcHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OneDriver.PowerSupply.Basic.GrpcHost.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OneDriver.PowerSupply.Basic.GrpcHost.dll"]

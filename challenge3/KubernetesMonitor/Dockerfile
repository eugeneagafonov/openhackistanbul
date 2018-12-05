FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY KubernetesMonitor.csproj KubernetesMonitor/
RUN dotnet restore KubernetesMonitor/KubernetesMonitor.csproj
COPY Program.cs KubernetesMonitor/
COPY . .
WORKDIR /src/KubernetesMonitor
RUN dotnet build KubernetesMonitor.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish KubernetesMonitor.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "KubernetesMonitor.dll"]

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["KubernetesApi/KubernetesApi.csproj", "KubernetesApi/"]
RUN dotnet restore "KubernetesApi/KubernetesApi.csproj"
COPY . .
WORKDIR "/src/KubernetesApi"
RUN dotnet build "KubernetesApi.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "KubernetesApi.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "KubernetesApi.dll"]
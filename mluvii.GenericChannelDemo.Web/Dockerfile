FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mluvii.GenericChannelDemo.Web/mluvii.GenericChannelDemo.Web.csproj", "mluvii.GenericChannelDemo.Web/"]
RUN dotnet restore "mluvii.GenericChannelDemo.Web/mluvii.GenericChannelDemo.Web.csproj"
COPY . .
WORKDIR "/src/mluvii.GenericChannelDemo.Web"
RUN dotnet build "mluvii.GenericChannelDemo.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "mluvii.GenericChannelDemo.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mluvii.GenericChannelDemo.Web.dll"]

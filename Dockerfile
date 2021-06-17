﻿FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["nxfw/nxfw.csproj", "nxfw/"]
RUN dotnet restore "nxfw/nxfw.csproj"
COPY . .
WORKDIR "/src/nxfw"
RUN dotnet build "nxfw.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "nxfw.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "nxfw.dll"]

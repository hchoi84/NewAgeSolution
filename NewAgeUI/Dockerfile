#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["NewAgeUI/NewAgeUI.csproj", "NewAgeUI/"]
COPY ["FileReaderLibrary/FileReaderLibrary.csproj", "FileReaderLibrary/"]
COPY ["SkuVaultLibrary/SkuVaultLibrary.csproj", "SkuVaultLibrary/"]
COPY ["EmailLibrary/EmailSenderLibrary.csproj", "EmailLibrary/"]
COPY ["ChannelAdvisorLibrary/ChannelAdvisorLibrary.csproj", "ChannelAdvisorLibrary/"]
RUN dotnet restore "NewAgeUI/NewAgeUI.csproj"
COPY . .
WORKDIR "/src/NewAgeUI"
RUN dotnet build "NewAgeUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NewAgeUI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "NewAgeUI.dll"]
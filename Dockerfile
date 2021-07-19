FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /src
COPY ["QuickChat/QuickChat.csproj", "QuickChat/"]
RUN dotnet restore "QuickChat/QuickChat.csproj"
COPY . .
WORKDIR "/src/QuickChat"
RUN dotnet build "QuickChat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "QuickChat.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "QuickChat.dll"]
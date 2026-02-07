FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["UserWindowsService/UserWindowsService.csproj", "UserWindowsService/"]
RUN dotnet restore "UserWindowsService/UserWindowsService.csproj"

COPY . .
WORKDIR "/src/UserWindowsService"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# create persistence folder
RUN mkdir -p /app/Data

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "UserWindowsService.dll"]

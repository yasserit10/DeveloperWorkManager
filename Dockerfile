FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["DeveloperWorkManager.csproj", "./"]
RUN dotnet restore "DeveloperWorkManager.csproj"
COPY . .
RUN dotnet restore "DeveloperWorkManager.csproj"
RUN dotnet tool restore
RUN dotnet publish "DeveloperWorkManager.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DeveloperWorkManager.dll"]

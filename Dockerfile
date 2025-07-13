# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy project files and restore dependencies
COPY . . 
RUN dotnet restore "Tetsing app 1.csproj"

# Build and publish the app
RUN dotnet publish "Tetsing app 1.csproj" -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Expose port 80 for HTTP traffic
EXPOSE 80

# Entry point for the application
ENTRYPOINT ["dotnet", "Tetsing app 1.dll"]

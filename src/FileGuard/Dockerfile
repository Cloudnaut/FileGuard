# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the source code and build the application
COPY . .
RUN dotnet publish -c Release -o out

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/out .

# Set the entry point for the application
ENTRYPOINT ["dotnet", "FileGuard.dll"]

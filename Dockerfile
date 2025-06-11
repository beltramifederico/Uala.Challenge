# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first for better Docker layer caching
COPY ["Uala.Challenge.sln", "."]
COPY ["Uala.Challenge.Api/Uala.Challenge.Api.csproj", "Uala.Challenge.Api/"]
COPY ["Uala.Challenge.Application/Uala.Challenge.Application.csproj", "Uala.Challenge.Application/"]
COPY ["Uala.Challenge.Domain/Uala.Challenge.Domain.csproj", "Uala.Challenge.Domain/"]
COPY ["Uala.Challenge.Infrastructure/Uala.Challenge.Infrastructure.csproj", "Uala.Challenge.Infrastructure/"]
COPY ["Uala.Challenge.UnitTests/Uala.Challenge.UnitTests.csproj", "Uala.Challenge.UnitTests/"]

# Restore dependencies
RUN dotnet restore "Uala.Challenge.sln"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR "/src/Uala.Challenge.Api"
RUN dotnet build "Uala.Challenge.Api.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "Uala.Challenge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published application
COPY --from=publish /app/publish .

# Expose the port that the application will run on
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Set the entry point
ENTRYPOINT ["dotnet", "Uala.Challenge.Api.dll"]

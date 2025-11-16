# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY *.sln ./
COPY src/ERPAccounting.API/*.csproj ./src/ERPAccounting.API/
COPY src/ERPAccounting.Application/*.csproj ./src/ERPAccounting.Application/
COPY src/ERPAccounting.Domain/*.csproj ./src/ERPAccounting.Domain/
COPY src/ERPAccounting.Infrastructure/*.csproj ./src/ERPAccounting.Infrastructure/
COPY src/ERPAccounting.Common/*.csproj ./src/ERPAccounting.Common/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build
WORKDIR /app/src/ERPAccounting.API
RUN dotnet build -c Release -o /app/build

# Publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "ERPAccounting.API.dll"]

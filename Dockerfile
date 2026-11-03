# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["AcxiomCRM.csproj", "./"]
RUN dotnet restore "AcxiomCRM.csproj"

# Copy all source code and publish release build
COPY . .
RUN dotnet publish "AcxiomCRM.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render assigns port dynamically via $PORT (defaults to 10000 on Render)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "AcxiomCRM.dll"]

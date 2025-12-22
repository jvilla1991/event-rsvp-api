# Use the official .NET 9 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["EventRsvp.Api/EventRsvp.Api.csproj", "EventRsvp.Api/"]
COPY ["EventRsvp.Application/EventRsvp.Application.csproj", "EventRsvp.Application/"]
COPY ["EventRsvp.Domain/EventRsvp.Domain.csproj", "EventRsvp.Domain/"]
COPY ["EventRsvp.Infrastructure/EventRsvp.Infrastructure.csproj", "EventRsvp.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "EventRsvp.Api/EventRsvp.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/EventRsvp.Api"
RUN dotnet build "EventRsvp.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "EventRsvp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "EventRsvp.Api.dll"]


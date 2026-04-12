# Stage 1: Build frontend
FROM node:lts-alpine AS frontend-build
WORKDIR /app/frontend
COPY src/NatsManager.Frontend/package*.json ./
RUN npm ci
COPY src/NatsManager.Frontend/ ./
RUN npm run build

# Stage 2: Build backend
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY NatsManager.slnx ./
COPY src/NatsManager.Domain/NatsManager.Domain.csproj src/NatsManager.Domain/
COPY src/NatsManager.Application/NatsManager.Application.csproj src/NatsManager.Application/
COPY src/NatsManager.Infrastructure/NatsManager.Infrastructure.csproj src/NatsManager.Infrastructure/
COPY src/NatsManager.Web/NatsManager.Web.csproj src/NatsManager.Web/
COPY src/NatsManager.ServiceDefaults/NatsManager.ServiceDefaults.csproj src/NatsManager.ServiceDefaults/
RUN dotnet restore src/NatsManager.Web/NatsManager.Web.csproj
COPY src/ src/
RUN dotnet publish src/NatsManager.Web/NatsManager.Web.csproj -c Release -o /app/publish --no-restore

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=backend-build /app/publish .
COPY --from=frontend-build /app/frontend/dist ./wwwroot
RUN mkdir -p /data && chown appuser:appgroup /data
USER appuser
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/data/natsmanager.db"
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "NatsManager.Web.dll"]

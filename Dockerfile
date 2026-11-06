FROM node:22-alpine AS web
WORKDIR /web
COPY web/package*.json ./
RUN npm ci
COPY web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY src/Flatplanner.Core/Flatplanner.Core.csproj src/Flatplanner.Core/
COPY src/Flatplanner.Api/Flatplanner.Api.csproj src/Flatplanner.Api/
RUN dotnet restore src/Flatplanner.Api
COPY src/ src/
RUN dotnet publish src/Flatplanner.Api -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
COPY --from=web /web/dist ./wwwroot
COPY prompts/ ./prompts/
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Flatplanner.Api.dll"]

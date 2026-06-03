# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore separado (aproveita cache de camadas quando só o código muda)
COPY global.json ./
COPY TodoApp.sln ./
COPY src/TodoApp.Domain/TodoApp.Domain.csproj         src/TodoApp.Domain/
COPY src/TodoApp.Application/TodoApp.Application.csproj src/TodoApp.Application/
COPY src/TodoApp.Infrastructure/TodoApp.Infrastructure.csproj src/TodoApp.Infrastructure/
COPY src/TodoApp.Api/TodoApp.Api.csproj                src/TodoApp.Api/
RUN dotnet restore src/TodoApp.Api/TodoApp.Api.csproj

# Código-fonte + scripts SQL (embutidos como recurso pela Infrastructure)
COPY src/ src/
COPY db/  db/
RUN dotnet publish src/TodoApp.Api/TodoApp.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TodoApp.Api.dll"]

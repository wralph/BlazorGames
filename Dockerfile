FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BlazorGames/BlazorGames.csproj", "BlazorGames/"]
RUN dotnet restore "BlazorGames/BlazorGames.csproj"
COPY . .
WORKDIR "/src/BlazorGames"
RUN dotnet build "BlazorGames.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorGames.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM nginx:alpine AS final
EXPOSE 80
EXPOSE 443
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app

EXPOSE 80


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ["nemestrix.csproj", "./"]

RUN dotnet restore "nemestrix.csproj"

COPY . .

RUN dotnet build "nemestrix.csproj" -c Release -o /app/build


FROM build as publish

RUN dotnet publish "nemestrix.csproj" -c Release -o /app/publish


FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

COPY docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENTRYPOINT ["/bin/bash", "-c", "/app/entrypoint.sh"]

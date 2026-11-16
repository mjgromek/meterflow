FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/MeterFlow.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir /data && chown $APP_UID /data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__MeterFlow="Data Source=/data/meterflow.db" \
    Export__Root=/data/exports
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "MeterFlow.Api.dll"]

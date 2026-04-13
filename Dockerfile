FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Execution.Gateway.csproj .
RUN dotnet restore
COPY Program.cs ApprovedIntent.cs DataSources.cs AgentMetrics.cs PrometheusMetricServer.cs IntentConsumerWorker.cs HeartbeatPublisher.cs ./
RUN dotnet publish -c Release -o /app/publish --no-restore /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 9090
ENV ConnectionStrings__Redis=redis:6379,abortConnect=false
ENV METRICS_PORT=9090
ENTRYPOINT ["dotnet", "Execution.Gateway.dll"]

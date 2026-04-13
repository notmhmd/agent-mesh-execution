FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY Execution.Gateway.csproj .
RUN dotnet restore
COPY Program.cs ApprovedIntent.cs DataSources.cs IntentConsumerWorker.cs HeartbeatPublisher.cs ./
RUN dotnet publish -c Release -o /app/publish --no-restore /p:PublishTrimmed=false

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ConnectionStrings__Redis=redis:6379,abortConnect=false
ENTRYPOINT ["dotnet", "Execution.Gateway.dll"]

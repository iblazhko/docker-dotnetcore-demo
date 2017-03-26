FROM microsoft/dotnet:1.1-runtime
WORKDIR /app
EXPOSE 5000
COPY _publish .
COPY appsettings.json .
ENTRYPOINT ["dotnet", "Client.dll"]

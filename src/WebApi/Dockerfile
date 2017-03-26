FROM microsoft/dotnet:1.1-runtime
WORKDIR /app
EXPOSE 5000
COPY _publish .
RUN mkdir ./logs
ENTRYPOINT ["dotnet", "WebApi.dll"]

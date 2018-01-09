FROM microsoft/dotnet:2.0-sdk AS build-env
WORKDIR /app
#COPY . .

#WORKDIR /app/ClientDataService.UnitTests
#RUN ["dotnet", "restore", "--source", "http://repo.concordservicing.com:8081/nexus/service/local/nuget/nuget-public/"]
#RUN ["dotnet", "test"]

#WORKDIR /app/SnapshotStore
#RUN ["dotnet", "restore", "--source", "http://repo.concordservicing.com:8081/nexus/service/local/nuget/nuget-public/"]
#RUN dotnet publish -c Release -o out

# Build runtime image
#FROM microsoft/aspnetcore:1.1
#WORKDIR /app
#COPY --from=build-env /app/ClientDataService/out/ .
#ENTRYPOINT ["dotnet", "ClientDataService.dll"]






# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN ["dotnet", "restore", "--force"]
#RUN dotnet restore

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# build runtime image
FROM microsoft/dotnet:2.0-runtime 
WORKDIR /app
COPY --from=build-env /app/out ./

ENV NUM_ACTORS=1
ENV FILENAME=""

ENTRYPOINT ["dotnet", "SnapShotStore.dll"]


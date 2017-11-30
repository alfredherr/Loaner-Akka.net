FROM microsoft/dotnet:1.1.2-sdk

WORKDIR /AkkaDotNetCoreDocker

#Copy all the source files and junk over to the container
COPY . .
#Just to make sure, show me
run ls -lt
#Now restore .net packages
RUN dotnet restore -r ubuntu-x86  
#Cut the release
RUN dotnet publish -c Release Lighthouse.NetCoreApp.csproj  
#How to run it
ENTRYPOINT dotnet "/AkkaDotNetCoreDocker/bin/Release/netcoreapp1.1/Lighthouse.NetCoreApp.dll" 


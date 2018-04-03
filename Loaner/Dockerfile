FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Loaner.csproj ./
RUN dotnet restore

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# build runtime image
FROM microsoft/dotnet:runtime
WORKDIR /app

# Set timezone to Phoenix
#RUN echo "America/Phoenix" > /etc/timezone
#RUN apt-get -y install htop
RUN apt-get -y install tzdata && ln -sf /usr/share/zoneinfo/America/Phoenix /etc/localtime
RUN dpkg-reconfigure -f noninteractive tzdata

COPY --from=build-env /app/out ./
COPY --from=build-env /app/ ./

EXPOSE 8080 7777 8443

ENTRYPOINT ["dotnet", "Loaner.dll"]

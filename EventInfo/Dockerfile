FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /EventInfo

# Copy everything
COPY . ./
RUN dotnet --info
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /EventInfo
COPY --from=build-env /EventInfo/out .
ENTRYPOINT ["dotnet", "EventInfo.dll"]
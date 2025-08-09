FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish
RUN #dotnet dev-certs https -ep /https/aspnetapp.pfx -p yourpassword

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

COPY --from=build /app/publish .
#COPY --from=build /https/aspnetapp.pfx /https/aspnetapp.pfx

EXPOSE 8443 
EXPOSE 5080


ENTRYPOINT ["dotnet", "AonFreelancing.dll"]
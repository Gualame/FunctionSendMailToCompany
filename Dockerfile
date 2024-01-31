FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
ENV TZ="America/Sao_Paulo"
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet build -c Release

EXPOSE 80

CMD ["dotnet", "run"]
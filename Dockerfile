FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY ./ReSharperConfigQuiz/ReSharperConfigQuiz.csproj .
RUN dotnet restore
COPY ./ReSharperConfigQuiz/ .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT dotnet ReSharperConfigQuiz.dll

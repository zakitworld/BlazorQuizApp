FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BlazorQuizApp.csproj", "."]
RUN dotnet restore "BlazorQuizApp.csproj"
COPY . .
RUN dotnet publish "BlazorQuizApp.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "BlazorQuizApp.dll"]

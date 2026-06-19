FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Alter_Parts/Alter_Parts.csproj", "Alter_Parts/"]
RUN dotnet restore "Alter_Parts/Alter_Parts.csproj"
COPY . .
WORKDIR "/src/Alter_Parts"
RUN dotnet build "Alter_Parts.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Alter_Parts.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Alter_Parts.dll"]
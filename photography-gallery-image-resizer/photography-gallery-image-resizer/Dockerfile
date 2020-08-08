FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 As base
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 As build

WORKDIR /src
COPY ["photography-gallery-image-resizer.csproj", ""]
RUN dotnet restore "./photography-gallery-image-resizer.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "photography-gallery-image-resizer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "photography-gallery-image-resizer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "photography-gallery-image-resizer.dll", "/images/input", "/images/output"]
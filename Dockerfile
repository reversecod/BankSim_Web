# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia os arquivos e restaura as dependências
COPY . ./
RUN dotnet restore

# Publica o projeto em modo Release
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Define a porta padrão
EXPOSE 80
ENTRYPOINT ["dotnet", "Banksim_Web.dll"]
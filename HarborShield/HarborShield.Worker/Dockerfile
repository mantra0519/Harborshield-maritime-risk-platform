# Build context is the solution folder (HarborShield/), not this directory:
#   docker build -f HarborShield.Worker/Dockerfile -t harborshield-worker .

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
# aspnet (not the plain runtime image) because HarborShield.ServiceDefaults has an explicit
# FrameworkReference to Microsoft.AspNetCore.App (Aspire's standard shared project), which the
# Worker pulls in transitively even though it doesn't host a web server itself.
# libgomp1 (OpenMP) is required by GGML/llama.cpp's native library (used by RiskCopilot), and
# libgssapi-krb5-2 is required by Npgsql for GSSAPI negotiation on connection open - neither
# ships in the minimal aspnet image by default.
RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["HarborShield.Worker/HarborShield.Worker.csproj", "HarborShield.Worker/"]
COPY ["HarborShield.Application/HarborShield.Application.csproj", "HarborShield.Application/"]
COPY ["HarborShield.Domain/HarborShield.Domain.csproj", "HarborShield.Domain/"]
COPY ["HarborShield.Infrastructure/HarborShield.Infrastructure.csproj", "HarborShield.Infrastructure/"]
COPY ["HarborShield.RiskCopilot/HarborShield.RiskCopilot.csproj", "HarborShield.RiskCopilot/"]
COPY ["HarborShield.Contracts/HarborShield.Contracts.csproj", "HarborShield.Contracts/"]
COPY ["HarborShield.ServiceDefaults/HarborShield.ServiceDefaults.csproj", "HarborShield.ServiceDefaults/"]
RUN dotnet restore "HarborShield.Worker/HarborShield.Worker.csproj"
COPY . .
WORKDIR "/src/HarborShield.Worker"
RUN dotnet build "HarborShield.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# -r linux-x64 (without --self-contained) keeps this framework-dependent but makes dotnet
# publish include RID-specific native assets - without it, LLamaSharp's native library
# (used by RiskCopilot) is silently omitted and fails to load at runtime.
RUN dotnet publish "HarborShield.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish -r linux-x64 --self-contained false /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HarborShield.Worker.dll"]

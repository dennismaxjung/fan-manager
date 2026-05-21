# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY FanManager.slnx ./
COPY src/FanManager.Core/*.csproj ./src/FanManager.Core/
COPY src/FanManager.Infrastructure/*.csproj ./src/FanManager.Infrastructure/
COPY src/FanManager/*.csproj ./src/FanManager/
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY .editorconfig ./

# Restore dependencies
RUN dotnet restore src/FanManager/FanManager.csproj

# Copy source code
COPY src/ ./src/

# Build and publish
WORKDIR /src/src/FanManager
RUN dotnet publish FanManager.csproj -c Release -o /app/publish --no-restore

# Runtime stage - based on nvidia/cuda for nvidia-smi support
FROM nvidia/cuda:12.9.2-base-ubuntu22.04 AS runtime

# Nvidia environment variables
ENV NVIDIA_VISIBLE_DEVICES=all
ENV NVIDIA_DRIVER_CAPABILITIES=utility,compute

# Install ipmitool needed
RUN apt-get update && \
    apt-get install -y ipmitool libicu70 && \
    rm -rf /var/lib/apt/lists/*

# Create app user
RUN useradd -m -s /bin/bash app && \
    mkdir -p /app && \
    chown -R app:app /app

WORKDIR /app
USER app

# Copy published app
COPY --from=build --chown=app:app /app/publish .


# Health check
HEALTHCHECK --interval=60s --timeout=10s --start-period=30s --retries=3 \
    CMD pgrep -f "FanManager" > /dev/null || exit 1

# Start application
ENTRYPOINT ["./FanManager"]

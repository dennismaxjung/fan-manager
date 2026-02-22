# Fan Manager for Dell PowerEdge Server with NVIDIA GPUs

Enterprise-grade fan controller for 13th Gen Dell PowerEdge Server using iDRAC. Controlling the fans by monitoring the temperature of passive cooled NVIDIA GPUs and the CPUs, built with C# and .NET 10 running in Docker.

## 🎯 Features

- ✅ **Testable Architecture** - Interfaces for all external dependencies (IIpmiService, INvidiaSmiService,
  IProcessService)
- ✅ **Clean Architecture** - Separation of Core, Infrastructure, and Worker layers
- ✅ **Modern C# 14** - Records, pattern matching, nullable reference types
- ✅ **Full Unit Test Coverage** - Demonstrates mocking with Moq
- ✅ **Docker Support** - Multi-stage build with nvidia/cuda base image
- ✅ **Production Ready** - Logging, health checks, graceful shutdown

## 🚀 Quick Start

### Prerequisites
- Docker
- Nvidia Container Runtime (for GPU support in Docker)
- Dell PowerEdge Server (Gen 13) with iDRAC 8 and `IPMI over LAN` Enabled
  - To configure IPMI over LAN, go to `Overview > iDRAC Settings > Network` in the iDRAC Web interface.
    Under IPMI Settings, specify the values for the attributes and click Apply.

### ⚙️ Configuration

Configuration via environment variables with prefix `FANMANAGER__`:

| Variable                                                                  | Default    | Description                               |
|---------------------------------------------------------------------------|------------|-------------------------------------------|
| `FANMANAGER__IPMI__HOST`                                                  | `local`    | iDRAC host ('local' or IP address)        |
| `FANMANAGER__IPMI__USERNAME`                                              | `root`     | iDRAC username                            |
| `FANMANAGER__IPMI__PASSWORD`                                              | `calvin`   | iDRAC password                            |
| `FANMANAGER__BASE_FAN_SPEED`                                              | `20`       | Base fan speed (%)                        |
| `FANMANAGER__CPU_TEMPERATURE_THRESHOLD`                                   | `60`       | CPU threshold (°C)                        |
| `FANMANAGER__GPU_TEMPERATURE_THRESHOLD`                                   | `45`       | GPU threshold (°C)                        |
| `FANMANAGER__GPU_TEMPERATURE_MAX`                                         | -          | Optional: Maximum GPU temperature (°C)    |
| `FANMANAGER__CHECK_INTERVAL`                                              | `00:00:30` | Check interval (TimeSpan format)          |
| `FANMANAGER__ENABLE_DELL_THIRD_PARTY_PCIE_CARD_COOLING_BEHAVIOR`          | `false`    | Enable Dell third-party PCIe card cooling |
| `FANMANAGER__RESTORE_DELL_THIRD_PARTY_PCIE_CARD_COOLING_BEHAVIOR_ON_EXIT` | `true`     | Restore Dell cooling behavior on exit     |

## Development

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run locally (requires IPMI and GPU access)
dotnet run --project src/FanManager/FanManager.csproj 
```

### Docker Build & Run

```bash
# Build Docker image
docker build -t fan-manager:latest .
```

### 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FanControlStrategyTests"
```

### 🏗️ Building from Source

```bash
# Clone repository
git clone <repo-url>
cd FanManager

# Restore dependencies
dotnet restore

# Build Release
dotnet build -c Release

# Publish self-contained
dotnet publish src/FanManager -c Release 
```

## 🐳 Docker Details

### Multi-Stage Build

1. **Build Stage** - Uses .NET SDK to compile
2. **Runtime Stage** - Uses nvidia/cuda with ipmitool installed

### Health Check

Container includes health check monitoring the worker process every 60 seconds.

---

## Best Practices Implemented

- ✅ **SOLID Principles** - Clean separation of concerns
- ✅ **Dependency Injection** - All dependencies injected
- ✅ **Interface Segregation** - Small, focused interfaces
- ✅ **Testability** - All business logic unit testable
- ✅ **Async/Await** - Proper async patterns
- ✅ **Nullable Reference Types** - Compile-time null safety
- ✅ **Records** - Immutable data structures
- ✅ **Pattern Matching** - Modern C# patterns
- ✅ **Structured Logging** - Microsoft.Extensions.Logging
- ✅ **Options Pattern** - Type-safe configuration with validation

---

## Roadmap

- **Web API** – A REST API to expose sensor values and stats for integration with dashboards like [Homepage](https://gethomepage.dev/) or [Home Assistant](https://www.home-assistant.io/)
- **Manual Override via Web API** – Ability to manually override fan settings through the API
- **GPU Selection** – Option to select specific NVIDIA GPUs to include/exclude (e.g. to exclude actively cooled GPUs)
- **Extended Hardware Metrics** – Additional server and GPU statistics via `nvidia-smi`, including GPU utilization and VRAM usage
- **Custom Fan Curve** – Support for defining a custom fan curve to fine-tune fan behavior based on temperature thresholds
- **Additional Device Support** – Support for other devices beyond GPUs that are cooled by the server fans (e.g. PCIe cards, storage, etc.)
- **Inlet & Exhaust Temperature** – Include inlet and exhaust air temperatures in the fan control logic for more accurate thermal management
- **Controller & Provider Architecture** – Architectural extension introducing a central controller and provider model, allowing resources running in other VMs or remote systems to report their metrics to the central controller
- **External Webhooks** – Ability to trigger external webhooks on temperature thresholds (e.g. to activate an air conditioning unit or send alerts)
- **Centralized Log Forwarding** – Support for forwarding logs to a central logging service (e.g. Loki, Graylog, or similar)

---

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any
contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also
simply open an issue.
Remember to give the project a star! Thanks again!

### Contributing Guidelines

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/123456_Amazing-Feature`)
3. Commit your Changes (`git commit -m 'feat(something): Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/123456_Amazing-Feature`)
5. Open a Pull Request

### Branch Naming

Branch naming conventions are enforced via git hooks. All branches must follow one of these patterns:

**Pattern 1:** `<type>/<ticket-number>_<description>`

- `type`: Must be one of: `bugfix`, `feature`, `impediment`
- `ticket-number`: 5 or more digits (e.g., `00001`)
- `description`: Lowercase alphanumeric with hyphens (e.g., `amazing-feature`)

**Pattern 2:** `develop`

- Protected main development branch

**Valid examples:**

- `feature/12345_add-gpu-monitoring`
- `bugfix/67890_fix-memory-leak`
- `impediment/11111_update-dependencies`
- `develop`

**Invalid examples:**

- `feature/1234_short-ticket` (ticket number is too short)
- `Feature/12345_uppercase` (type must be lowercase)
- `feature/12345_Invalid_Name` (description must be lowercase with hyphens only)

### Commit Messages

We follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification for all commit
messages. This standard is enforced via git hooks.

**Format:** `<type>(<scope>): <description>`

**Commit message length:** Maximum 100 characters

**Common types:**

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `test`: Test additions or modifications
- `chore`: Maintenance tasks

---

## 📄  License

Inspired
by: [tigerblue77/Dell_iDRAC_fan_controller_Docker](https://github.com/tigerblue77/Dell_iDRAC_fan_controller_Docker)

Distributed under the MIT License. See [LICENSE](./LICENSE) for more information.

### Legal Disclaimer

This project is an independent open-source utility.

- **iDRAC** and **PowerEdge** are registered trademarks of **Dell Inc**.
- **NVIDIA** is a registered trademark of **NVIDIA Corporation**.

This software is not affiliated with, sponsored by, or endorsed by Dell Inc. or NVIDIA Corporation. Use it at your own
  risk.

---

**Built with ❤️ using .NET 10 and C# 14**

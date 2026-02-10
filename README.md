# ARES OS 2.0: Autonomous Research Software
The next generation of autonomous research software, serving as the central hub for integrating devices, planners, and analyzers for self-directed scientific campaigns. This is our general research software, if you're interested in specifically our 3-D printer software, please visit the [Educational ARES Repo](https://github.com/AFRL-ARES/Educational-ARES).

## 🎛️ Overview
**ARES OS 2.0** acts as the core central hub that connects and coordinates components such as devices, planners, and analyzers into cohesive research campaigns. Built on a modular, distributed architecture, ARES provides the framework for defining complex, multi-step experiments and managing all connected systems from a unified dashboard.

## ✨ Key Features
### Architecture
- **ARES UI (Web Client)**: The user interface for defining campaigns, monitoring progress, and interacting with devices via a standard web browser.
- **ARES Service (Back-End)**: The core component that hosts business logic, manages the component lifecycle of devices, planners, and analyzers, and executes campaign orchestration.

### System Integration and Extensibility
* **Centralized Orchestration:** ARES connects and manages the flow of data and control between Planners (AI/logic), Analyzers (post-processing), and Devices (physical hardware).
* **Cross-Platform Support:** ARES OS is compatible with Linux, macOS and Windows
* **Language Agnostic API:** Protobuf and gRPC provide a language agnostic API for integrating with ARES. Full support for creating custom components is provided for:
  * C#: Using the native ARES libraries
  * Python: Thanks to the PyARES library (See Related Projects)
* **Component Management:** Devices, Planners, and Analyzers can be added, removed and edited easily via dedicated settings menus within the ARES software.

### Campaign Creation
- **Experiment Templates**: Users craft complex research campaigns by defining **Experiment Templates.**
- **Template Components**: Each template specifies:
  - **Planning Parameters**: The values you're asking your planner to provide for your experiments.
  - **Startup Script**: A set of steps executed once at the beginning of an experiment before running the first iteration of your experiment template.
  - **Experiment Script**: The core logic of your campaign. A set of defined steps ARES executes with every iteration of your experiment.
  - **Closeout Script**: The closeout logic of your campaign. Ensures all hardware is set to an optimal state before finishing a campaign. This logic is also used in the event a campaign fails to complete.
  - **Analysis Assignment**: Analyzers define their specific input needs, and ARES allows you to define what values from your campaign will match these expected values.
  - **Planner Assignment**: The assignment of your planning parameters to specific planning services.

### Dashboard & Control
- **Unified Dashboard**: A single web interface for defining campaigns, monitoring execution progress and more.
- **Direct Device Control**: Users can directly control and monitor the status of all added devices from the ARES Dashboard.

## 🛠️ Installation & Deployment
ARES OS 2.0 offers two deployment methods: the simple [ARES Launcher](https://github.com/AFRL-ARES/ARES-Launcher) desktop app for local use, or manual build/run for custom environments.

### Option 1: Using the ARES Launcher (Recommended)
For the easiest local deployment, use the ARES Launcher desktop application. Download the latest release for your operating system from the [ARES Launcher Releases](https://github.com/AFRL-ARES/ARES-Launcher/releases). Follow the documentation outlined in the ARES Launcher repository.

### Option 2: Manual Build and Run
**Prerequisites**
- **.NET 10 SDK** (Required for all components)
- **Entity Framework Core Tools**: Required for managing database migrations
  ```bash
  dotnet tool install --global dotnet-ef
  ```
- **An Integrated Development Environment (IDE):**
  - **Recommendation: Visual Studio 2026** for the best C# and Blazor development experience.

**Steps**
1. **Clone the repository:**
  ```Bash
  git clone https://github.com/AFRL-ARES/ARES.git
  cd ARES
  ```
2. **Trust HTTPS Development Certificate**
  ```Bash
  dotnet dev-certs https --trust
  ```
3. **Restore Dependencies and Build**
  ```Bash
  dotnet restore
  dotnet build
  ```
4. **Initialize Database**
  ```Bash
  dotnet run --project .\AresService\AresService.csproj --migrate
  ```
5. **Start ARES**
```Bash
dotnet run --project .\AresService\AresService.csproj
dotnet run --project .\UI\UI.csproj
```
6. **Navigate to the UI** <br></br>
Open a browser of your choice and navigate to [https://localhost:7084](https://localhost:7084). If you've started ARES successfully, this will open the ARES Dashboard.

## 🔗 Related Projects and References
ARES OS 2.0 relies on the following repositories for its operation foundation, component creation, and simplified installation:
- **ARES Launcher:** Desktop application for installing and managing local ARES instances with a simple UI. **(Downloadable Releases Available)**
  - https://github.com/AFRL-ARES/ARES-Launcher
- **ARES Datamodel:** Defines the universal **Protobuf/gRPC contracts** used for communication between the ARES Service, UI, and all custom components.
  - https://github.com/AFRL-ARES/ARES-Datamodel
- **PyARES:** The official ARES Python library for building custom planners, analyzers and devices, enabling support beyond the native C# environment.
  - https://github.com/AFRL-ARES/PyARES

## CLEARANCE
Distribution A. Approved for public release: distribution unlimited. AFRL-2025-5329

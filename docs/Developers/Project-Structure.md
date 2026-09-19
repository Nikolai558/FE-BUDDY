# FE-BUDDY 3.0 Project Structure

> **Aspirational — not the current build target.** This Clean Architecture layout is a
> post-GUI migration target. The active build (Airways services and everything that leads
> up to the GUI) uses the `SERVICES` layout described in
> [`FE-Buddy_3.0_Structure_And_Build_Plan.md`](FE-Buddy_3.0_Structure_And_Build_Plan.md).
> Do not relocate existing `FEBuddyLibrary` code into `Domain`/`Application`/`Infrastructure`/
> `Desktop` projects until that document says the migration phase has started.

FE-BUDDY 3.0 follows a layered architecture inspired by common .NET Clean Architecture practices.  
The goal is to keep business logic, external integrations, and the user interface separated so the application remains maintainable, testable, and easier to expand.

## Repository Structure

```text
FE-BUDDY/
│
├── src/
│   │
│   ├── FeBuddy.Domain/
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Enums/
│   │   └── Exceptions/
│   │
│   ├── FeBuddy.Application/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   └── Mappings/
│   │
│   ├── FeBuddy.Infrastructure/
│   │   ├── FAA/
│   │   │   ├── NASR/
│   │   │   │   ├── Models/
│   │   │   │   ├── Parsers/
│   │   │   │   ├── Downloads/
│   │   │   │   └── Services/
│   │   │   │
│   │   │   └── DTTP/
│   │   │       ├── Models/
│   │   │       ├── Parsers/
│   │   │       ├── Downloads/
│   │   │       └── Services/
│   │   │
│   │   ├── Repositories/
│   │   ├── FileSystem/
│   │   ├── Networking/
│   │   ├── Serialization/
│   │   ├── Logging/
│   │   ├── Configuration/
│   │   └── Updates/
│   │
│   └── FeBuddy.Desktop/
│       ├── Views/
│       ├── ViewModels/
│       ├── Controls/
│       ├── Converters/
│       ├── Services/
│       ├── Configuration/
│       ├── Resources/
│       └── Styles/
│
├── tests/
│   ├── FeBuddy.Domain.Tests/
│   ├── FeBuddy.Application.Tests/
│   └── FeBuddy.Infrastructure.Tests/
│
├── installer/
│   └── FeBuddy.Installer/
│
├── docs/
├── .github/
│
├── FE-BUDDY.sln
├── Directory.Build.props
├── .editorconfig
├── .gitignore
├── README.md
└── LICENSE
```


## Project Responsibilities

### FeBuddy.Domain

Contains the core aviation/domain objects and rules used throughout FE-BUDDY.

Examples include:

- Airports
- Runways
- Navaids
- Fixes
- Airways
- Boundaries
- Coordinates
- AIRAC concepts

The Domain project should have minimal external dependencies and should not know anything about WPF, files, HTTP, FAA downloads, or specific data formats.


### FeBuddy.Application

Contains the application's use cases and coordinates operations using the Domain.

Examples include:

- AIRAC operations
- Data conversion workflows
- Airport data operations
- Validation
- Application-level services

Interfaces for external functionality can be defined here so the Application layer does not need to know how that functionality is implemented.


### FeBuddy.Infrastructure

Contains implementations that communicate with systems outside FE-BUDDY.

Examples include:

- FAA NASR data
- FAA DTTP data
- CSV parsing
- XML/JSON serialization
- HTTP requests
- File system access
- Downloads
- Logging
- Application updates

Infrastructure converts external data into representations understood by the Domain and Application layers.


### FeBuddy.Desktop

Contains the WPF user interface and MVVM implementation.

Examples include:

- Views
- ViewModels
- Custom controls
- UI converters
- Navigation
- Dialogs
- Themes
- Styles
- Images and other UI resources

The Desktop project should primarily handle presentation and user interaction rather than implementing aviation or FAA data-processing logic.


### Tests

Tests are separated from production code and generally mirror the architecture being tested.

- `FeBuddy.Domain.Tests` tests domain behavior.
- `FeBuddy.Application.Tests` tests application workflows and services.
- `FeBuddy.Infrastructure.Tests` tests parsers, data handling, serialization, and external integration behavior.


## Dependency Direction

The intended dependency direction is:

    FeBuddy.Desktop
          │
          ▼
    FeBuddy.Application
          │
          ▼
      FeBuddy.Domain
          ▲
          │
    FeBuddy.Infrastructure

`FeBuddy.Domain` is the center of the application and should not depend on the other projects.

`FeBuddy.Application` depends on the Domain.

`FeBuddy.Infrastructure` provides implementations for external systems required by the Application.

`FeBuddy.Desktop` provides the user interface and uses the Application layer to perform work.


## General Organization Rule

Folders should describe a class's responsibility rather than act as generic storage locations.

Prefer specific responsibilities such as:

- Services
- Parsers
- Repositories
- Providers
- Validators
- Converters
- Factories
- DTOs
- Interfaces
- Extensions

Avoid broad catch-all folders such as `Helpers`, `Misc`, or `Common` unless the contained functionality is genuinely generic and does not have a more appropriate architectural responsibility.

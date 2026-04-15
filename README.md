# CoveoValidator

An ASP.NET MVC 5 web application (.NET Framework 4.8) that validates Coveo GA (Global Attributes) content for Sitecore items. It searches the Coveo index using CQL queries, deserializes the raw GA field JSON, and checks whether each expected field is present, empty, or missing — using the domain model as the source of truth.

## Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8 SDK
- IIS Express

## Setup

1. **Clone the repository**

2. **Add your Coveo bearer token** — open `CoveoValidator/Web.config` and set:
   ```xml
   <add key="Coveo:DefaultBearerToken" value="<your token here>" />
   ```
   The token is read server-side only and is never exposed in the UI.

3. **Restore NuGet packages** — right-click the solution in Visual Studio → *Restore NuGet Packages*, or run:
   ```
   nuget restore CoveoValidator.sln
   ```

4. **Run** — press F5 in Visual Studio. IIS Express starts on `http://localhost:50416/`.

## Usage

1. Select a **Content Type** (Hotel or Excursion).
2. Enter one or more **Sriggle IDs** (comma or semicolon separated).
3. Click **Search** to query Coveo and see per-field validation results.

Each field is reported as:

| Status | Meaning |
|---|---|
| **Valid** | Present in GA data with a non-empty value |
| **Empty** | Present but value is null, whitespace, `[]`, or `{}` |
| **Missing** | Expected by the domain model but not found in GA data |

## Architecture

```
CoveoValidator/
├── Controllers/CoveoController.cs       — GET/POST Index
├── Services/CoveoService.cs             — Coveo API calls + validation logic
├── Helpers/FieldValidationHelper.cs     — Reflection-based field comparison
├── Models/Domain/                       — HotelInfoModel, ExcursionInfoModel (validation schema)
├── Models/Coveo/                        — API request/response shapes
└── Views/Coveo/Index.cshtml             — Search form + results table
```

**Validation schema:** `HotelInfoModel` and `ExcursionInfoModel` define the expected fields. Adding or renaming a property in those classes automatically changes what gets validated.

**Coveo field mapping:**

| Content Type | GA field |
|---|---|
| Hotel | `fgahotelinfo50416` |
| Excursion | `fgaez120xcursioninfo50416` |

All queries filter to English content under `/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/`.

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

1. Select a **Content Type** (Hotel or Excursion). Changing the content type clears the Sriggle IDs field.
2. Select a **Language** (`English (en)` or `Arabic (ar-AE)`).
3. Enter one or more **Sriggle IDs** (comma or semicolon separated). The Search button is enabled only when at least one ID is entered.
4. Click **Search** to query Coveo and view per-field validation results in an accordion.
5. Click **Export to Excel** (visible when results are present) to download a `.xlsx` report.

Each field is reported as:

| Status | Meaning |
|---|---|
| **Valid** | Present in GA data with a non-empty value |
| **Empty** | Present but value is null, whitespace, `[]`, or `{}` |
| **Missing** | Expected by the domain model but not found in GA data |
| **Not Found** | Coveo returned no document for the given Sriggle ID |

## Excel Export

The exported workbook contains two sheets:

**Summary** — one row per Sriggle ID with columns: SriggleId, Title, ItemId, ContentType, Language, Missing count, Empty count, Valid count, Overall Status, Missing Field Names, Empty Field Names. Status cells are colour-coded (red = missing/error, yellow = empty, green = valid, grey = not found).

**Field Detail** — one row per field with columns: SriggleId, Title, ContentType, FieldName, Status, Value. Not-found and error items are excluded.

## Architecture

```
CoveoValidator/
├── Controllers/CoveoController.cs       — GET/POST Index, POST ExportExcel
├── Services/CoveoService.cs             — Coveo API calls + validation logic
├── Services/ExcelExportService.cs       — Excel workbook generation (EPPlus 4.5.3)
├── Helpers/FieldValidationHelper.cs     — Reflection-based field comparison
├── Models/Domain/                       — HotelInfoModel, ExcursionInfoModel (validation schema)
├── Models/Coveo/                        — API request/response shapes
└── Views/Coveo/Index.cshtml             — Search form + results accordion
```

**Validation schema:** `HotelInfoModel` and `ExcursionInfoModel` (both extending `SriggleBaseModel`) define the expected fields. Adding or renaming a property automatically changes what gets validated.

**Coveo field mapping:**

| Content Type | Template GUID | Backend field | GA field |
|---|---|---|---|
| Hotel | `d801ccec-bb25-4621-a1db-ea4eaf32120e` | `fhotelinfo50416` | `fgahotelinfo50416` |
| Excursion | `44002bda-145b-49a9-a8f4-36b568362c76` | `fez120xcursioninfo50416` | `fgaez120xcursioninfo50416` |

All queries filter by the user-selected language and the path `/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/`. Sriggle IDs are matched using `@fsriggleid50416==` (exact match).

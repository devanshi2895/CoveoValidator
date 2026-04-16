# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

**Prerequisites:** Visual Studio 2019+, .NET Framework 4.8 SDK, IIS Express.

**Restore packages before first build:**
```
nuget restore CoveoValidator.sln
```

Or via Package Manager Console in Visual Studio (required for EPPlus after a fresh clone):
```
Update-Package EPPlus -reinstall
```

**Build:**
```
msbuild CoveoValidator.sln /p:Configuration=Debug
```

**Run:** Open in Visual Studio 2019 and press F5. IIS Express starts on `http://localhost:50416/`. Default route lands on `CoveoController/Index`.

**No test project exists** in this solution.

## Configuration

`Web.config` is the only config file. The Coveo bearer token lives there:
```xml
<add key="Coveo:DefaultBearerToken" value="..." />
```
Read server-side only via `ConfigurationManager.AppSettings` — never passed through the UI or exposed to the client.

## Architecture

### Request flows

**Search (`POST /Coveo`):**
```
CoveoController.Index
  → splits comma/semicolon SriggleIds, deduplicates
  → reads bearer token from Web.config
  → CoveoService.SearchAndValidateAsync (Task.WhenAll — one call per ID)
      → BuildQueryBody     constructs CQL (language + path + templateId + @fsriggleid50416)
      → Coveo Search API v2 (POST https://platform.cloud.coveo.com/rest/search/v2)
      → ParseAndValidate   deserializes response, extracts backend/GA field JSON
          → DeserializeFieldItems  handles mixed-type values (string/int/bool/array/object/null)
          → FieldValidationHelper.Validate<TModel>  reflection walk of domain model
  → CoveoSearchViewModel → Index.cshtml
```

**Export (`POST /Coveo/ExportExcel`):**
```
CoveoController.ExportExcel
  → same search logic as above (no view rendered)
  → ExcelExportService.Export
      → Sheet "Summary"      one row per item, colour-coded Overall Status + field name lists
      → Sheet "Field Detail" one row per field with Status and Value
  → FileResult (.xlsx download)
```

### CQL query structure

```
@flanguage50416=="<language>"
AND @ffullpath50416=*"/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/*"
AND @ftemplateid50416=="<templateGuid>"
AND @fsriggleid50416=="<id>"
```

Language is user-selected (`en` or `ar-AE`). Sriggle ID uses exact match (`==`) on the dedicated `@fsriggleid50416` field.

### Result states

`ComparisonResultModel` carries one of four mutually exclusive states:

| Flag | Meaning |
|---|---|
| `IsNotFound = true` | Coveo returned 0 documents for the SriggleId — placeholder accordion shown, no fields to validate |
| `HasError = true` | API or parse error — `ErrorMessage` holds detail |
| `HasMissing / HasEmpty` | Document found, GA fields validated against model |
| all false | All expected fields are valid |

When `ParseAndValidate` receives an empty Coveo result set it returns an `IsNotFound` placeholder rather than an empty list, so the accordion always has an entry for every searched ID.

### Domain models as validation schema

`HotelInfoModel` and `ExcursionInfoModel` (both extending `SriggleBaseModel`) define the **expected GA fields**. `FieldValidationHelper` walks the full inheritance chain via reflection and compares property names (case-insensitive) against `{name, value}` pairs from `fgahotelinfo50416` / `fgaez120xcursioninfo50416`. Each field is classified Missing, Empty, or Valid.

To add or rename a validated field, update the corresponding domain model class. `SriggleBaseModel` contributes `SriggleId`, `SriggleCode`, `SriggleName` to every type.

### Content types and Coveo field mapping

| ContentType | Template GUID | Backend field | GA field |
|---|---|---|---|
| Hotel | `d801ccec-bb25-4621-a1db-ea4eaf32120e` | `fhotelinfo50416` | `fgahotelinfo50416` |
| Excursion | `44002bda-145b-49a9-a8f4-36b568362c76` | `fez120xcursioninfo50416` | `fgaez120xcursioninfo50416` |

### Excel export (ExcelExportService)

Uses **EPPlus 4.5.3** (LGPL, single DLL, no transitive dependencies). `ExcelExportService.Export` returns a `byte[]` consumed directly by `Controller.File(...)`.

- **Summary sheet** — columns 1–9: identity + counts + Overall Status; columns 10–11: comma-separated Missing Field Names and Empty Field Names (colour-coded, capped at 60 chars wide). `IsNotFound` and error rows have blank field-name columns.
- **Field Detail sheet** — one row per field; `IsNotFound` and error items are skipped entirely.

### DeserializeFieldItems

Handles double-encoded Coveo responses — when the field value is itself a JSON string wrapping an array, it unwraps and recurses. All value tokens are normalised to `string`. An empty result causes all expected fields to be flagged Missing.

### Key constraints

- **C# 7.3** (`<LangVersion>7.3</LangVersion>`). Expression-bodied members and null-conditional `?.` are allowed; switch expressions, records, and other C# 8+ features are not.
- **Razor MVC 5 parser quirk**: inside a `@for` loop body, declare only a single `var` before the first HTML element. Move computed logic into model properties (see `ComparisonResultModel` computed helpers) to avoid the Razor parser rendering C# as literal text. Use plain `<form>` tags inside `@if` blocks — `using (Html.BeginForm(...))` inside a code block is unreliable in MVC 5 Razor.
- `System.Configuration` must remain in the `.csproj` `<Reference>` list — it is not auto-included in MVC 5 projects.
- Bootstrap JS CDN link intentionally has **no SRI integrity hash** — the hash caused silent load failures in some environments.

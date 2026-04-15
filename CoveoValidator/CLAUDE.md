# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

**Prerequisites:** Visual Studio 2019, .NET Framework 4.8 SDK, IIS Express.

**Restore packages before first build:**
Right-click Solution → Restore NuGet Packages (or `nuget restore CoveoValidator.sln`).

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
The token is read server-side only via `ConfigurationManager.AppSettings`. It is never passed through the UI or exposed to the client.

## Architecture

### Request flow

```
POST /Coveo → CoveoController.Index
  → splits comma/semicolon SriggleIds, deduplicates
  → reads bearer token from Web.config
  → CoveoService.SearchAndValidateAsync (Task.WhenAll — one call per ID)
    → BuildQueryBody   — constructs CQL query (language + path + templateId + sriggleId filter)
    → Coveo Search API v2 (POST https://platform.cloud.coveo.com/rest/search/v2)
    → ParseAndValidate — deserializes response, extracts backend/GA field JSON
      → DeserializeFieldItems — handles mixed-type values (string/int/bool/array/object/null)
      → FieldValidationHelper.Validate<TModel> — reflection walk of domain model properties
  → CoveoSearchViewModel → Index.cshtml
```

### Domain models as validation schema

`HotelInfoModel` and `ExcursionInfoModel` (both extending `SriggleBaseModel`) define the **expected GA fields**. `FieldValidationHelper` walks the full inheritance chain via reflection and compares property names (case-insensitive) against the deserialized `{name, value}` pairs from `fgahotelinfo50416` / `fgaez120xcursioninfo50416`. Each field is classified:

- **Missing** — property exists in the model but no matching name in GA data
- **Empty** — present but value is null / whitespace / `[]` / `{}`
- **Valid** — present with meaningful content

To add or rename a validated field, update the corresponding domain model class.

### Content types and Coveo field mapping

| ContentType | Template GUID | Backend field | GA field |
|---|---|---|---|
| Hotel | `d801ccec-...` | `fhotelinfo50416` | `fgahotelinfo50416` |
| Excursion | `44002bda-...` | `fez120xcursioninfo50416` | `fgaez120xcursioninfo50416` |

CQL filters applied to every query: `@flanguage50416=="en"` and `@ffullpath50416=*"/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/*"`.

### ItemId extraction

`ExtractItemIdFromUri` parses the Sitecore URI format `sitecore://database/web/ItemId/{GUID}/Language/en/Version/N` and is preferred over the `fitemid50416` raw field as the primary source of ItemId.

### Key constraints

- **C# 7.3** (`<LangVersion>7.3</LangVersion>` in .csproj). Expression-bodied members and null-conditional `?.` are allowed; newer features (switch expressions, records, etc.) are not.
- **Razor MVC 5 parser quirk**: inside a `@for` loop body, declare only a single `var` before the first HTML element. Move all computed logic into model properties to avoid the Razor parser rendering C# declarations as literal text.
- `System.Configuration` must remain in the `.csproj` `<Reference>` list — it is not included automatically in MVC 5 projects.
- Bootstrap JS CDN link intentionally has **no SRI integrity hash** — the hash caused silent load failures in some environments, breaking all accordion/tab interactivity.

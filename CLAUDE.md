# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

This workspace contains **CoveoValidator** (`CoveoValidator/`), an ASP.NET MVC 5 web application (.NET Framework 4.8) that validates Coveo GA (Global Attributes) content for Sitecore items. A more detailed `CLAUDE.md` lives in `CoveoValidator/`.

## Build & Run

**Prerequisites:** Visual Studio 2019+, .NET Framework 4.8 SDK, IIS Express.

**Restore NuGet packages:**
```
nuget restore CoveoValidator/CoveoValidator.sln
```

**Build:**
```
msbuild CoveoValidator/CoveoValidator.sln /p:Configuration=Debug
```

**Run:** Open `CoveoValidator/CoveoValidator.sln` in Visual Studio and press F5. IIS Express starts on `http://localhost:50416/`. Default route lands on `CoveoController/Index`.

**No test project exists.**

## Configuration

`CoveoValidator/Web.config` is the only config file. The Coveo bearer token lives there:
```xml
<add key="Coveo:DefaultBearerToken" value="..." />
```
Read server-side only via `ConfigurationManager.AppSettings` — never exposed to the client.

## Architecture

### Request flow

```
POST /Coveo → CoveoController.Index
  → splits comma/semicolon SriggleIds, deduplicates
  → reads bearer token from Web.config
  → CoveoService.SearchAndValidateAsync (Task.WhenAll — one call per ID)
      → BuildQueryBody     constructs CQL query (language + path + templateId + sriggleId)
      → Coveo Search API v2 (POST https://platform.cloud.coveo.com/rest/search/v2)
      → ParseAndValidate   deserializes response, extracts backend/GA field JSON
          → DeserializeFieldItems  handles mixed-type values (string/int/bool/array/object/null)
          → FieldValidationHelper.Validate<TModel>  reflection walk of domain model
  → CoveoSearchViewModel → Index.cshtml
```

### Domain models as validation schema

`HotelInfoModel` and `ExcursionInfoModel` (both extending `SriggleBaseModel`) define the **expected GA fields**. `FieldValidationHelper` walks the full inheritance chain via reflection and compares property names case-insensitively against `{name, value}` pairs from the GA field. Each field is classified **Missing**, **Empty**, or **Valid**.

To add or rename a validated field, update the corresponding domain model class.

### Content types and Coveo field mapping

| ContentType | Template GUID | Backend field | GA field |
|---|---|---|---|
| Hotel | `d801ccec-bb25-4621-a1db-ea4eaf32120e` | `fhotelinfo50416` | `fgahotelinfo50416` |
| Excursion | `44002bda-145b-49a9-a8f4-36b568362c76` | `fez120xcursioninfo50416` | `fgaez120xcursioninfo50416` |

CQL filters on every query: `@flanguage50416=="en"` and `@ffullpath50416=*"/sitecore/content/YasConnect/GlobalContent/SharedContent/Platform/*"`.

`ExtractItemIdFromUri` parses `sitecore://database/web/ItemId/{GUID}/Language/en/Version/N` and is preferred over the raw `fitemid50416` field.

## Key Constraints

- **C# 7.3** (`<LangVersion>7.3</LangVersion>`). No switch expressions, records, or other C# 8+ features.
- **Razor MVC 5 parser quirk**: inside a `@for` loop body, declare only a single `var` before the first HTML element. Move computed logic into model properties.
- `System.Configuration` must be kept in the `.csproj` `<Reference>` list — it is not auto-included in MVC 5 projects.
- Bootstrap JS CDN link intentionally has **no SRI integrity hash** — the hash caused silent load failures in some environments.

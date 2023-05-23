# WKG Base

![](https://git.wkg.lan/WKG/components/wkg-base/badges/main/pipeline.svg)

---

`Wkg` is a company-internal library providing reusable components for the development of any .NET project.

## Installation

The `Wkg` library is available as a NuGet package from our internal nuget feed. To install it, add the following package source to your NuGet configuration:

```xml
<PropertyGroup>
    <RestoreAdditionalProjectSources>
        https://baget.wkg.lan/v3/index.json
    </RestoreAdditionalProjectSources>
</PropertyGroup>
```

Then, install the package by adding the following package reference to your project file:

```xml
<ItemGroup>
    <PackageReference Include="Wkg" Version="X.X.X" />
</ItemGroup>
```

> **Note**
> Replace `X.X.X` with the latest stable version available on the [nuget feed](https://baget.wkg.lan/packages/wkg/latest).

## Usage

For technical documentation and usage examples, please refer to the [documentation](/docs/documentation.md).
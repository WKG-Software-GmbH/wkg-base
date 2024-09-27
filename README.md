# WKG Base

[![NuGet](https://img.shields.io/badge/NuGet-555555?style=for-the-badge&logo=nuget)![NuGet version (Wkg)](https://img.shields.io/nuget/v/Wkg.svg?style=for-the-badge&label=Wkg)![NuGet downloads (Wkg)](https://img.shields.io/nuget/dt/Wkg?style=for-the-badge)](https://www.nuget.org/packages/Wkg/)

---

`Wkg` is a company-internal library providing reusable components and utilities for the development of any .NET application in our projects. Its core features include QoS-aware task scheduling, lightweight logging, and a set of reflection, threading, and performance utilities.

As part of our commitment to open-source software, we are making this library [available to the public](https://github.com/WKG-Software-GmbH/wkg-base) under the GNU General Public License v3.0. We hope that it will be useful to other developers and that the community will contribute to its further development.

## Installation

Install the `Wkg` NuGet package by adding the following package reference to your project file:

```xml
<ItemGroup>
    <PackageReference Include="Wkg" Version="X.X.X" />
</ItemGroup>
```

> :warning: **Warning**
> Replace `X.X.X` with the latest stable version available on the [nuget feed](https://www.nuget.org/packages/Wkg), where **the major version must match the major version of your targeted .NET runtime**.

## Usage

For technical documentation and usage examples, please refer to the [documentation](https://github.com/WKG-Software-GmbH/wkg-base/tree/main/docs/documentation.md).
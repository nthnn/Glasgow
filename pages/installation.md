## Installation

Glasgow can be easily installed into your .NET project using NuGet Package Manager.

### Installation via NuGet Package Manager

The simplest way to add Glasgow to your project is through NuGet.

<p align="center">
    <img alt="Glasgow on NuGet" src="../assets/nuget-screenshot.png" />
</p>

### Using the .NET CLI

Open your terminal or command prompt, navigate to your project's directory, and run the following command:

```bash
dotnet add package Glasgow --version 1.0.0
```

### Using Visual Studio

1. Open your project in Visual Studio.
2. Right-click on your project in the Solution Explorer and select "Manage NuGet Packages...".
3. In the "Browse" tab, search for `Glasgow`.
4. Select the `Glasgow` package and click "Install".
5. Confirm the installation when prompted.

### Using PowerShell (Package Manager Console in Visual Studio)

If you're in Visual Studio, you can also use the Package Manager Console:

1. Go to `Tools` > `NuGet Package Manager` > `Package Manager Console`.
2. In the console, run the following command:

    ```powershell
    Install-Package Glasgow -Version 1.0.0
    ```

3. After installation, you can verify that the package is correctly referenced in your project file (e.g., `.csproj`). Look for an `<PackageReference>` entry similar to this:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Glasgow" Version="1.0.0" />
  </ItemGroup>

</Project>
```

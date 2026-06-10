# LauncherShyax

![Windows Build](https://github.com/Hordekiller/Launcher/actions/workflows/windows-build.yml/badge.svg)

A simple launcher for **World of Warcraft** that locates the game path, checks server status, generates the `realmlist.wtf` file, and downloads update files when needed.

---

## ✨ Features

- 🔍 Automatically locates the game installation path from the **Windows Registry**
- 🌐 Checks the **server status** before launching
- 📝 Creates and manages the `realmlist.wtf` file safely
- ⬇️ Downloads update files when required
- 🧹 Safer handling of `realmlist.wtf` and `Cache` files

## 🚀 What's New

- Migrated to **`net8.0-windows`** with **WinForms**
- Network and download logic rewritten using **`async/await`** and **`HttpClient`**
- Game path now read directly from the **Windows Registry**
- Improved, safer handling of `realmlist.wtf` and `Cache`

## 📦 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Windows** (required for building and running the WinForms app)

## 🛠️ Build and Run

Build the project:

```bash
dotnet build LauncherShyax.csproj
```

Run it (on Windows):

```bash
dotnet run --project LauncherShyax.csproj
```

Produce a release build:

```bash
dotnet publish LauncherShyax.csproj --configuration Release --output publish
```

## 📄 License

This project is licensed under the **[GNU GPL v3](https://www.gnu.org/licenses/gpl-3.0)** or any later version.

## ⚠️ Note

- The `bin/` and `obj/` folders are **not** tracked in this repository.
- Full functionality requires **Windows** and access to the **World of Warcraft installation path**.

<!-- CI trigger: 2026-06-10T15:56:47Z -->
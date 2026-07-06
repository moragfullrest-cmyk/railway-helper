# Публикация в NuGet

## Предварительные требования

1. Аккаунт на [nuget.org](https://www.nuget.org/)
2. API key с правами **Push** (Account → API Keys)
3. Уникальный `PackageId` — при публикации в публичный feed замените `your-org` в `Directory.Build.props` и `RailwayHelper.csproj` на реальный URL репозитория

## Локальная сборка пакета

```powershell
cd railway-helper
dotnet restore RailwayHelper.sln
dotnet build RailwayHelper.sln -c Release --no-restore
dotnet test RailwayHelper.sln -c Release --no-build
dotnet pack src/RailwayHelper/RailwayHelper.csproj -c Release --no-build -o ./artifacts
```

Или одной командой:

```powershell
.\scripts\pack.ps1
```

В каталоге `artifacts/` появятся:

- `RailwayHelper.1.0.0.nupkg` — основной пакет
- `RailwayHelper.1.0.0.snupkg` — символы для отладки

## Публикация

```powershell
$apiKey = $env:NUGET_API_KEY  # или передайте явно
dotnet nuget push ./artifacts/RailwayHelper.*.nupkg `
  --source https://api.nuget.org/v3/index.json `
  --api-key $apiKey `
  --skip-duplicate
```

## Версионирование

Версия задаётся в `src/RailwayHelper/RailwayHelper.csproj` (`<Version>`).

Перед релизом:

1. Обновите версию в `.csproj`
2. Добавьте запись в `CHANGELOG.md`
3. Соберите и опубликуйте пакет

## CI/CD (GitHub Actions)

Пример workflow — `.github/workflows/ci.yml`:

- `dotnet build` + `dotnet test` на push/PR
- `dotnet pack` на tag `v*`
- `dotnet nuget push` с секретом `NUGET_API_KEY`

## Приватный feed

Для Azure Artifacts, GitHub Packages или локального feed укажите свой `--source`:

```powershell
dotnet nuget push ./artifacts/RailwayHelper.*.nupkg --source "https://pkgs.dev.azure.com/org/_packaging/feed/nuget/v3/index.json" --api-key $apiKey
```

## Checklist перед первой публикацией

- [ ] `PackageId` уникален на nuget.org
- [ ] `Authors`, `Description`, `RepositoryUrl` заполнены
- [ ] `README.md` и `LICENSE` включены в пакет
- [ ] `dotnet test` проходит
- [ ] Пример `RailwayHelper.Samples` запускается

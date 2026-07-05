# CLAUDE.md — FitnessQuest

Gamifierad fitness-app för Android (.NET 10 MAUI). Se `README.md` för funktioner.

## Release-rutin (VIKTIGT) — automatisk

**Varje push till `main` skapar automatiskt en ny release** via GitHub Actions (`.github/workflows/release.yml`):

1. Räknar ut nästa version (ökar patch från senaste taggen, t.ex. `v1.8` → `v1.8.1`).
2. Kör enhetstester, bygger en **signerad** Release-APK (versionen sätts från taggen; `ApplicationVersion` = run-number).
3. Skapar taggen `vX.Y.Z` och en **GitHub Release** med APK:n bifogad.

Så det enda som behövs är att committa och pusha till `main`:
```bash
git push origin main   # → CI taggar, bygger, signerar och släpper automatiskt
```

- Vill du hoppa över release för en viss commit: skriv `[skip release]` i commit-meddelandet.
- Vill du sätta en specifik version manuellt: pusha en tagg själv (`git tag -a v2.0.0 -m ...; git push origin v2.0.0`).
- **Google Drive-kopia** (`D:\Min enhet\FitnessQuest\`) är en lokal distributionsvana — CI gör inte detta; APK:n laddas annars ner från GitHub-releasen.

> Signering sker med en fast keystore lagrad som GitHub Secrets (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`). Keystore-backup finns i `D:\Min enhet\FitnessQuest\keystore\`. Tappas den kan appen aldrig uppdateras.
> Det lokala bygg-kommandot nedan behövs bara för att verifiera på emulator innan push.

## Bygg-kommando

`JAVA_HOME`/`ANDROID_HOME` är inte satta på maskinen – ange sökvägarna explicit:

- JDK: `C:\Program Files\Android\Android Studio\jbr`
- Android SDK: `%LOCALAPPDATA%\Android\Sdk`
- Emulator (AVD) för test: `PostningsMall_Pixel`

```bash
dotnet publish -f net10.0-android -c Release \
  "-p:JavaSdkDirectory=C:\Program Files\Android\Android Studio\jbr" \
  "-p:AndroidSdkDirectory=%LOCALAPPDATA%\Android\Sdk" \
  -p:AndroidPackageFormat=apk
```
Signerad APK: `bin/Release/net10.0-android/publish/com.obos.fitnessquest-Signed.apk`.

> OBS: Debug-APK går inte att sidoladda manuellt (Fast Deployment) – använd Release-APK för installation/test.

## Stack
.NET 10 MAUI (Android), `MauiVersion` pinnad till 10.0.60, CommunityToolkit.Mvvm, CommunityToolkit.Maui, sqlite-net-pcl (lokal lagring), ZXing.Net.Maui (streckkod), egna diagram via Microsoft.Maui.Graphics. App-id `com.obos.fitnessquest`.

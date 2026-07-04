# CLAUDE.md — FitnessQuest

Gamifierad fitness-app för Android (.NET 10 MAUI). Se `README.md` för funktioner.

## Release-rutin (VIKTIGT)

**Vid varje ny version ska följande alltid göras:**

1. **Bumpa versionen** i `FitnessQuest.csproj` (`ApplicationDisplayVersion` + `ApplicationVersion`). Versionen visas på startsidan.
2. **Committa och pusha** till GitHub (`origin` = https://github.com/5ynchronizeD/FitnessQuest.git, branch `main`).
3. **Skapa och pusha en tagg** `vX.Y` (matchande versionen). Detta triggar GitHub Actions (`.github/workflows/release.yml`) som kör tester, bygger en Release-APK och **skapar en GitHub Release automatiskt** med APK:n bifogad.
   ```bash
   git tag -a v1.7 -m "FitnessQuest v1.7"
   git push origin v1.7
   ```
4. **Kopiera APK:n till Google Drive** (`D:`): `D:\Min enhet\FitnessQuest\FitnessQuest-<version>.apk` (lokal distribution — CI gör inte detta). Alternativt laddas APK:n ner från GitHub-releasen.

> CI bygger själva releasen; det lokala bygg-kommandot nedan behövs bara för att testa/verifiera på emulator innan du taggar.

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

# CLAUDE.md — FitnessQuest

Gamifierad fitness-app för Android (.NET 10 MAUI). Se `README.md` för funktioner.

## Release-rutin (VIKTIGT)

**Vid varje ny version ska följande alltid göras:**

1. **Bumpa versionen** i `FitnessQuest.csproj` (`ApplicationDisplayVersion` + `ApplicationVersion`). Versionen visas på startsidan.
2. **Bygg en Release-APK** (se bygg-kommando nedan).
3. **Kopiera APK:n till Google Drive** (`D:`): `D:\Min enhet\FitnessQuest\FitnessQuest-<version>.apk`.
4. **Committa och pusha till GitHub** (`origin` = https://github.com/5ynchronizeD/FitnessQuest.git, branch `main`).

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

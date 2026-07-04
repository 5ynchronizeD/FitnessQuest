# FitnessQuest 🏋️

En gamifierad fitness-app för Android, byggd med **.NET MAUI**. Kombinerar kost och träning med XP, nivåer, streaks och bragder för att göra det roligt att fortsätta logga.

## Funktioner

### 🎮 Gamification
- XP, nivåer och rank (Nybörjare → Legend)
- Daglig streak med bonus-XP
- 15 upplåsbara bragder över kost, gym, cardio och nivå
- Firande med XP-toast och level-up-dialoger

### 🥗 Kost
- Streckkodsskanning → näringsvärden från Open Food Facts
- Sök på namn samt senaste & favoriter
- Gram-väljare med live makro-preview, måltidsval
- Redigera loggade måltider i efterhand

### 🏋️ Gym (Strong-likt)
- Sökbar övningskatalog med muskelgrupp & utrustning
- Pass-editor med set-rader (kg/reps/klar), pass-timer och volym
- Set-typer (uppvärmning/dropset/failure), superset, vilotimer
- **Viktkalkylator** med egen skivuppsättning (antal per skiva), separata lägen för skivstång och hantlar
- Redigera tidigare pass fullständigt

### 🏃 Cardio
- Löpning / cykling / promenad med distans, tid och live-tempo
- Redigera och ta bort pass

### 📊 Statistik
- Kalorier senaste 7 dagarna (med mållinje)
- Träningsvolym per vecka, cardio-distans
- Övningsprogression (beräknat 1RM över tid)

## Teknik
- .NET 10, .NET MAUI (Android)
- MVVM med CommunityToolkit.Mvvm
- SQLite (sqlite-net-pcl) för lokal lagring
- ZXing.Net.Maui för streckkodsskanning
- Egna diagram ritade med Microsoft.Maui.Graphics

## Bygga
```bash
dotnet build -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```
APK hamnar i `bin/Release/net10.0-android/publish/`.

using FitnessQuest.Models;

namespace FitnessQuest.Data;

/// <summary>Common exercises seeded on first launch. Users can add their own.</summary>
public static class ExerciseCatalog
{
    public static List<Exercise> Seed() => new()
    {
        // Bröst
        E("Bänkpress", "Bröst", Equipment.Barbell),
        E("Hantelpress", "Bröst", Equipment.Dumbbell),
        E("Lutande bänkpress", "Bröst", Equipment.Barbell),
        E("Lutande hantelpress", "Bröst", Equipment.Dumbbell),
        E("Flyes", "Bröst", Equipment.Dumbbell),
        E("Kabelcross", "Bröst", Equipment.Cable),
        E("Dips", "Bröst", Equipment.Bodyweight),

        // Rygg
        E("Marklyft", "Rygg", Equipment.Barbell),
        E("Skivstångsrodd", "Rygg", Equipment.Barbell),
        E("Latsdrag", "Rygg", Equipment.Cable),
        E("Sittande kabelrodd", "Rygg", Equipment.Cable),
        E("Pull-ups", "Rygg", Equipment.Bodyweight),
        E("Hantelrodd", "Rygg", Equipment.Dumbbell),
        E("T-bar rodd", "Rygg", Equipment.Machine),

        // Ben
        E("Knäböj", "Ben", Equipment.Barbell),
        E("Frontböj", "Ben", Equipment.Barbell),
        E("Benpress", "Ben", Equipment.Machine),
        E("Rumänsk marklyft", "Ben", Equipment.Barbell),
        E("Utfall", "Ben", Equipment.Dumbbell),
        E("Benspark", "Ben", Equipment.Machine),
        E("Lårcurl", "Ben", Equipment.Machine),
        E("Vadpress", "Ben", Equipment.Machine),

        // Axlar
        E("Militärpress", "Axlar", Equipment.Barbell),
        E("Axelpress hantlar", "Axlar", Equipment.Dumbbell),
        E("Sidolyft", "Axlar", Equipment.Dumbbell),
        E("Face pulls", "Axlar", Equipment.Cable),
        E("Framåtlyft", "Axlar", Equipment.Dumbbell),

        // Armar
        E("Bicepscurl", "Armar", Equipment.Dumbbell),
        E("Skivstångscurl", "Armar", Equipment.Barbell),
        E("Hammercurl", "Armar", Equipment.Dumbbell),
        E("Tricepspress kabel", "Armar", Equipment.Cable),
        E("Skullcrushers", "Armar", Equipment.Barbell),
        E("Tricepsdips", "Armar", Equipment.Bodyweight),

        // Mage
        E("Plankan", "Mage", Equipment.Bodyweight),
        E("Situps", "Mage", Equipment.Bodyweight),
        E("Hängande benlyft", "Mage", Equipment.Bodyweight),
        E("Cable crunch", "Mage", Equipment.Cable),
    };

    private static Exercise E(string name, string muscle, Equipment eq) =>
        new() { Name = name, MuscleGroup = muscle, Equipment = eq };
}

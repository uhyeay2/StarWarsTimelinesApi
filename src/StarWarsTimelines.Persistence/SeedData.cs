using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StarWarsTimelines.Domain.Entities;
using StarWarsTimelines.Domain.Enums;

namespace StarWarsTimelines.Persistence;

/// <summary>
/// Seeds the database with demo users, the source material catalog, representative source material units with
/// sample user progress, sample library entries, and the timeline of events with their character, location, and
/// vehicle links.
/// </summary>
/// <remarks>
/// Seeding is idempotent: each section is skipped when its table already contains data, so it is safe to call on
/// every application start after migrations are applied.
/// </remarks>
public static class SeedData
{
    /// <summary>The fixed identifier of the seeded administrator account.</summary>
    public static readonly Guid AdminUserId = new("11111111-1111-1111-1111-111111111111");

    /// <summary>The fixed identifier of the seeded demo account "padme".</summary>
    public static readonly Guid PadmeUserId = new("22222222-2222-2222-2222-222222222222");

    /// <summary>The fixed identifier of the seeded demo account "luke".</summary>
    public static readonly Guid LukeUserId = new("33333333-3333-3333-3333-333333333333");

    /// <summary>The fixed identifier of the seeded demo account "rey".</summary>
    public static readonly Guid ReyUserId = new("44444444-4444-4444-4444-444444444444");

    /// <summary>
    /// Seeds the users, catalog, sample libraries, and timeline if their tables are empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    public static void Seed(AppDbContext db)
    {
        SeedUsers(db);
        SeedSourceMaterials(db);
        SeedUnits(db);
        SeedUnitProgress(db);
        SeedLibraries(db);
        SeedTimeline(db);
        db.SaveChanges();
    }

    /// <summary>
    /// Seeds the demo user accounts if the users table is empty, and backfills email addresses and verification for
    /// any existing accounts that predate the email feature.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedUsers(AppDbContext db)
    {
        if (!db.Users.Any())
        {
            var hasher = new PasswordHasher<object>();
            var now = DateTime.UtcNow;

            db.Users.AddRange(
                SeedUser(AdminUserId, "admin", "Admin", "admin123", UserRole.Admin, now, hasher),
                SeedUser(PadmeUserId, "padme", "Padmé Amidala", "padme123", UserRole.Standard, now, hasher),
                SeedUser(LukeUserId, "luke", "Luke Skywalker", "luke123", UserRole.Standard, now, hasher),
                SeedUser(ReyUserId, "rey", "Rey", "rey123", UserRole.Standard, now, hasher));
        }

        BackfillSeedEmails(db);
    }

    /// <summary>
    /// Assigns demo email addresses and marks accounts as verified when they predate the email feature, so that the
    /// seed accounts remain able to log in.
    /// </summary>
    /// <param name="db">The database context used to update seed data.</param>
    private static void BackfillSeedEmails(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        foreach (var user in db.Users.Where(x => x.Email == string.Empty))
        {
            user.Email = $"{user.Username.ToLowerInvariant()}@example.com";
            user.EmailVerifiedAtUtc ??= now;
        }
    }

    /// <summary>
    /// Builds a single seed user with a hashed password.
    /// </summary>
    /// <param name="id">The fixed identifier of the user.</param>
    /// <param name="username">The user's login name.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="password">The plain-text password to hash.</param>
    /// <param name="role">The user's role.</param>
    /// <param name="now">The timestamp used for the creation date.</param>
    /// <param name="hasher">The password hasher used to hash the password.</param>
    /// <returns>A fully populated seed user.</returns>
    private static User SeedUser(Guid id, string username, string displayName, string password, UserRole role, DateTime now, PasswordHasher<object> hasher) =>
        new()
        {
            Id = id,
            Username = username,
            DisplayName = displayName,
            Email = $"{username}@example.com",
            EmailVerifiedAtUtc = now,
            PasswordHash = hasher.HashPassword(null!, password),
            Role = role,
            CreatedAtUtc = now
        };

    /// <summary>
    /// Seeds the source material catalog if the table is empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedSourceMaterials(AppDbContext db)
    {
        if (db.SourceMaterials.Any())
        {
            return;
        }

        db.SourceMaterials.AddRange(
            SeedMaterial(1, "Star Wars: Episode I - The Phantom Menace", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(2, "Star Wars: Episode II - Attack of the Clones", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(3, "Star Wars: Episode III - Revenge of the Sith", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(4, "Star Wars: Episode IV - A New Hope", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(5, "Star Wars: Episode V - The Empire Strikes Back", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(6, "Star Wars: Episode VI - Return of the Jedi", Medium.Movie, CanonType.CanonAndLegends),
            SeedMaterial(7, "Star Wars: Episode VII - The Force Awakens", Medium.Movie, CanonType.Canon),
            SeedMaterial(8, "Star Wars: Episode VIII - The Last Jedi", Medium.Movie, CanonType.Canon),
            SeedMaterial(9, "Star Wars: Episode IX - The Rise of Skywalker", Medium.Movie, CanonType.Canon),
            SeedMaterial(10, "Star Wars: The Clone Wars", Medium.AnimatedShow, CanonType.Canon),
            SeedMaterial(11, "Star Wars: Rebels", Medium.AnimatedShow, CanonType.Canon),
            SeedMaterial(12, "The Mandalorian", Medium.LiveActionShow, CanonType.Canon),
            SeedMaterial(13, "Ahsoka", Medium.LiveActionShow, CanonType.Canon),
            SeedMaterial(14, "Dawn of the Jedi", Medium.Comic, CanonType.Legends),
            SeedMaterial(15, "The Old Republic: Revan", Medium.Book, CanonType.Legends),
            SeedMaterial(16, "Darth Bane: Path of Destruction", Medium.Book, CanonType.Legends),
            SeedMaterial(17, "Darth Plagueis", Medium.Book, CanonType.Legends),
            SeedMaterial(18, "The High Republic: Light of the Jedi", Medium.Book, CanonType.Canon),
            SeedMaterial(19, "Shatterpoint", Medium.Book, CanonType.Legends),
            SeedMaterial(20, "Legacy of the Force: Betrayal", Medium.Book, CanonType.Legends),
            SeedMaterial(21, "Star Wars: Knights of the Old Republic", Medium.VideoGame, CanonType.Legends),
            SeedMaterial(22, "Star Wars Jedi: Fallen Order", Medium.VideoGame, CanonType.Canon));
    }

    /// <summary>
    /// Builds a single seed source material with a deterministic identifier derived from the sequence number.
    /// </summary>
    /// <param name="sequence">A 1-based sequence number used to derive the fixed identifier.</param>
    /// <param name="title">The display title of the material.</param>
    /// <param name="medium">The medium of the material.</param>
    /// <param name="canonType">The continuity of the material.</param>
    /// <returns>A fully populated seed source material.</returns>
    private static SourceMaterial SeedMaterial(int sequence, string title, Medium medium, CanonType canonType) =>
        new()
        {
            Id = new Guid($"00000000-0000-0000-0000-{sequence:D12}"),
            Title = title,
            Medium = medium,
            CanonType = canonType
        };

    /// <summary>
    /// Seeds representative sub-units for a handful of source materials if the units table is empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedUnits(AppDbContext db)
    {
        if (db.SourceMaterialUnits.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var index = 0;
        foreach (var item in SeedUnitData)
        {
            index++;
            db.SourceMaterialUnits.Add(new SourceMaterialUnit
            {
                Id = UnitId(item.MaterialSequence, item.Number),
                SourceMaterialId = new Guid($"00000000-0000-0000-0000-{item.MaterialSequence:D12}"),
                UnitType = item.UnitType,
                Number = item.Number,
                Title = item.Title,
                CreatedAtUtc = now
            });
        }
    }

    /// <summary>
    /// Seeds sample per-unit progress for demo users if the progress table is empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedUnitProgress(AppDbContext db)
    {
        if (db.UserSourceMaterialUnits.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        // Padme has watched three of the five seeded Clone Wars episodes while keeping the item In Progress.
        db.UserSourceMaterialUnits.AddRange(
            SeedProgress(PadmeUserId, 10, 1, true, now),
            SeedProgress(PadmeUserId, 10, 2, true, now),
            SeedProgress(PadmeUserId, 10, 3, true, now));
    }

    /// <summary>
    /// Builds a single seed progress record with a deterministic unit identifier.
    /// </summary>
    /// <param name="userId">The identifier of the owning user.</param>
    /// <param name="materialSequence">The 1-based catalog sequence of the unit's source material.</param>
    /// <param name="number">The unit number being tracked.</param>
    /// <param name="isCompleted">Whether the unit is completed.</param>
    /// <param name="now">The timestamp used for the last-updated date.</param>
    /// <returns>A fully populated seed progress record.</returns>
    private static UserSourceMaterialUnit SeedProgress(Guid userId, int materialSequence, int number, bool isCompleted, DateTime now) =>
        new()
        {
            UserId = userId,
            SourceMaterialUnitId = UnitId(materialSequence, number),
            IsCompleted = isCompleted,
            UpdatedAtUtc = now
        };

    /// <summary>
    /// Resolves the deterministic identifier of a seed unit from its source material sequence and number.
    /// </summary>
    /// <param name="materialSequence">The 1-based catalog sequence of the unit's source material.</param>
    /// <param name="number">The unit number.</param>
    /// <returns>The fixed identifier of the matching seed unit.</returns>
    private static Guid UnitId(int materialSequence, int number)
    {
        var index = 0;
        for (var i = 0; i < SeedUnitData.Length; i++)
        {
            if (SeedUnitData[i].MaterialSequence == materialSequence && SeedUnitData[i].Number == number)
            {
                index = i;
                break;
            }
        }

        return new Guid($"00000000-0000-0000-0000-{500000000000 + index + 1:D12}");
    }

    /// <summary>
    /// Describes a seed unit and which source material it belongs to.
    /// </summary>
    /// <param name="MaterialSequence">The 1-based catalog sequence of the owning source material.</param>
    /// <param name="UnitType">The kind of unit.</param>
    /// <param name="Number">The unit's position within its source material.</param>
    /// <param name="Title">The optional display title of the unit.</param>
    private sealed record SeedUnitEntry(int MaterialSequence, UnitType UnitType, int Number, string? Title);

    /// <summary>
    /// Gets the representative units to seed for a handful of source materials.
    /// </summary>
    private static readonly SeedUnitEntry[] SeedUnitData =
    [
        // The Clone Wars (10)
        new(10, UnitType.Episode, 1, null),
        new(10, UnitType.Episode, 2, null),
        new(10, UnitType.Episode, 3, null),
        new(10, UnitType.Episode, 4, null),
        new(10, UnitType.Episode, 5, null),
        // The Mandalorian (12)
        new(12, UnitType.Episode, 1, "Chapter 1: The Mandalorian"),
        new(12, UnitType.Episode, 2, "Chapter 2: The Child"),
        new(12, UnitType.Episode, 3, "Chapter 3: The Sin"),
        new(12, UnitType.Episode, 4, "Chapter 4: Sanctuary"),
        new(12, UnitType.Episode, 5, "Chapter 5: The Gunslinger"),
        new(12, UnitType.Episode, 6, "Chapter 6: The Prisoner"),
        new(12, UnitType.Episode, 7, "Chapter 7: The Reckoning"),
        new(12, UnitType.Episode, 8, "Chapter 8: Redemption"),
        // Ahsoka (13)
        new(13, UnitType.Episode, 1, "Part One: Master and Apprentice"),
        new(13, UnitType.Episode, 2, "Part Two: Toil and Trouble"),
        new(13, UnitType.Episode, 3, "Part Three: Time to Fly"),
        // Dawn of the Jedi (14)
        new(14, UnitType.Issue, 1, null),
        new(14, UnitType.Issue, 2, null),
        new(14, UnitType.Issue, 3, null),
        // Shatterpoint (19)
        new(19, UnitType.Chapter, 1, null),
        new(19, UnitType.Chapter, 2, null),
        new(19, UnitType.Chapter, 3, null),
        // Star Wars Jedi: Fallen Order (22)
        new(22, UnitType.Level, 1, null),
        new(22, UnitType.Level, 2, null),
        new(22, UnitType.Level, 3, null)
    ];

    /// <summary>
    /// Seeds sample library entries if the library table is empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedLibraries(AppDbContext db)
    {
        if (db.UserSourceMaterials.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        db.UserSourceMaterials.AddRange(
            SeedLibraryItem(PadmeUserId, 1, TrackingStatus.Completed, true, now),
            SeedLibraryItem(PadmeUserId, 2, TrackingStatus.InProgress, false, now),
            SeedLibraryItem(PadmeUserId, 10, TrackingStatus.InProgress, false, now),
            SeedLibraryItem(PadmeUserId, 9, TrackingStatus.WishListed, false, now),
            SeedLibraryItem(PadmeUserId, 16, TrackingStatus.Completed, true, now),
            SeedLibraryItem(PadmeUserId, 17, TrackingStatus.WishListed, false, now),
            SeedLibraryItem(PadmeUserId, 21, TrackingStatus.WishListed, false, now),
            SeedLibraryItem(LukeUserId, 4, TrackingStatus.Completed, true, now),
            SeedLibraryItem(LukeUserId, 5, TrackingStatus.Completed, true, now));
    }

    /// <summary>
    /// Builds a single seed library entry with a deterministic source material identifier.
    /// </summary>
    /// <param name="userId">The identifier of the owning user.</param>
    /// <param name="sourceSequence">The 1-based catalog sequence of the tracked source material.</param>
    /// <param name="status">The initial progress status.</param>
    /// <param name="isFavorite">Whether the item is a favorite.</param>
    /// <param name="now">The timestamp used for the creation date.</param>
    /// <returns>A fully populated seed library entry.</returns>
    private static UserSourceMaterial SeedLibraryItem(Guid userId, int sourceSequence, TrackingStatus status, bool isFavorite, DateTime now) =>
        new()
        {
            UserId = userId,
            SourceMaterialId = new Guid($"00000000-0000-0000-0000-{sourceSequence:D12}"),
            Status = status,
            IsFavorite = isFavorite,
            CreatedAtUtc = now
        };

    /// <summary>
    /// Seeds the character, location, and vehicle lookups and the timeline events if the events table is empty.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    private static void SeedTimeline(AppDbContext db)
    {
        if (db.SourceMaterialEvents.Any())
        {
            return;
        }

        var characters = SeedCharacters(db);
        var locations = SeedLocations(db);
        var vehicles = SeedVehicles(db);

        var index = 0;
        foreach (var item in TimelineEvents)
        {
            index++;
            db.SourceMaterialEvents.Add(new SourceMaterialEvent
            {
                Id = new Guid($"00000000-0000-0000-0000-{400000000000 + index:D12}"),
                Title = item.Title,
                Description = item.Description,
                CanonType = item.CanonType,
                Year = item.Year,
                DisplayDate = item.DisplayDate,
                SourceMaterialId = new Guid($"00000000-0000-0000-0000-{item.MaterialSequence:D12}"),
                EventCharacters = item.Characters.Select(name => new EventCharacter { CharacterId = characters[name] }).ToList(),
                EventLocations = item.Locations.Select(name => new EventLocation { LocationId = locations[name] }).ToList(),
                EventVehicles = item.Vehicles.Select(name => new EventVehicle { VehicleId = vehicles[name] }).ToList()
            });
        }
    }

    /// <summary>
    /// Seeds every character referenced by the timeline with a deterministic identifier.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    /// <returns>A mapping from character name to its seeded identifier.</returns>
    private static Dictionary<string, Guid> SeedCharacters(AppDbContext db) =>
        SeedLookup(db.Characters, TimelineEvents.SelectMany(e => e.Characters).Distinct().ToArray(), 100_000_000_000, (name, id) => new Character { Id = id, Name = name });

    /// <summary>
    /// Seeds every location referenced by the timeline with a deterministic identifier.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    /// <returns>A mapping from location name to its seeded identifier.</returns>
    private static Dictionary<string, Guid> SeedLocations(AppDbContext db) =>
        SeedLookup(db.Locations, TimelineEvents.SelectMany(e => e.Locations).Distinct().ToArray(), 200_000_000_000, (name, id) => new Location { Id = id, Name = name });

    /// <summary>
    /// Seeds every vehicle referenced by the timeline with a deterministic identifier.
    /// </summary>
    /// <param name="db">The database context used to insert seed data.</param>
    /// <returns>A mapping from vehicle name to its seeded identifier.</returns>
    private static Dictionary<string, Guid> SeedVehicles(AppDbContext db) =>
        SeedLookup(db.Vehicles, TimelineEvents.SelectMany(e => e.Vehicles).Distinct().ToArray(), 300_000_000_000, (name, id) => new Vehicle { Id = id, Name = name });

    /// <summary>
    /// Inserts the given names into a lookup table with deterministic identifiers derived from a base value, in
    /// order of first appearance across the timeline.
    /// </summary>
    /// <typeparam name="TEntity">The lookup entity type being seeded.</typeparam>
    /// <param name="dbSet">The table to insert the entries into.</param>
    /// <param name="names">The names to seed, in deterministic identifier order.</param>
    /// <param name="baseId">The base used to derive each fixed identifier.</param>
    /// <param name="factory">A factory that builds an entity from its name and fixed identifier.</param>
    /// <returns>A mapping from name to its seeded identifier.</returns>
    private static Dictionary<string, Guid> SeedLookup<TEntity>(
        DbSet<TEntity> dbSet,
        IReadOnlyList<string> names,
        long baseId,
        Func<string, Guid, TEntity> factory)
        where TEntity : class
    {
        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Count; i++)
        {
            var id = new Guid($"00000000-0000-0000-0000-{baseId + i + 1:D12}");
            dbSet.Add(factory(names[i], id));
            map[names[i]] = id;
        }

        return map;
    }

    /// <summary>
    /// Describes a seed timeline event and which source material and lookup entries it references.
    /// </summary>
    /// <param name="Title">The display title of the event.</param>
    /// <param name="Description">A human-readable summary of what happened during the event.</param>
    /// <param name="CanonType">The continuity the event belongs to.</param>
    /// <param name="Year">The numeric year of the event on the galactic timeline.</param>
    /// <param name="DisplayDate">The formatted display date of the event.</param>
    /// <param name="MaterialSequence">The 1-based catalog sequence of the source material the event is drawn from.</param>
    /// <param name="Characters">The names of the characters that appear in the event.</param>
    /// <param name="Locations">The names of the locations the event takes place in.</param>
    /// <param name="Vehicles">The names of the vehicles that appear in the event.</param>
    private sealed record SeedEventData(
        string Title,
        string Description,
        CanonType CanonType,
        int Year,
        string DisplayDate,
        int MaterialSequence,
        string[] Characters,
        string[] Locations,
        string[] Vehicles);

    /// <summary>
    /// Gets the timeline events to seed, mirroring the frontend timeline dataset.
    /// </summary>
    private static readonly SeedEventData[] TimelineEvents =
    [
        new(
            "Origins of the Jedi Order",
            "Force-sensitive pilgrims gather on Tython, beginning the study of the light side and founding the ancient order that would become the Jedi.",
            CanonType.Legends,
            -25000,
            "c. 25,000 BBY",
            14,
            ["The Prime Jedi", "Rusk"],
            ["Tython"],
            []),
        new(
            "Revan and the Exile of the Sith",
            "The former Jedi Revan hunts the Sith Emperor across the Outer Rim, facing a darkness far older than the Republic ever imagined.",
            CanonType.Legends,
            -3954,
            "c. 3,954 BBY",
            15,
            ["Revan", "Scourge", "The Sith Emperor"],
            ["Dromund Kaas", "Coruscant"],
            ["Ebon Hawk", "Interdictor-class cruiser"]),
        new(
            "The Ruusan Reformation",
            "With the Sith believed destroyed on Ruusan, the Republic disbands the Army of Light as Darth Bane rebuilds the Sith in secret.",
            CanonType.Legends,
            -1000,
            "c. 1,000 BBY",
            16,
            ["Darth Bane", "Lord Kaan"],
            ["Ruusan", "Coruscant"],
            ["Acclamator-class assault ship"]),
        new(
            "The Invasion of Naboo",
            "The Trade Federation blockades and invades Naboo, setting the stage for the return of the Sith and the rise of Anakin Skywalker.",
            CanonType.CanonAndLegends,
            -32,
            "32 BBY",
            1,
            ["Qui-Gon Jinn", "Obi-Wan Kenobi", "Padme Amidala", "Darth Maul"],
            ["Naboo", "Otoh Gunga", "Theed"],
            ["Radiant VII", "Sith Infiltrator", "Naboo N-1 starfighter"]),
        new(
            "The Battle of Geonosis",
            "The first great battle of the Clone Wars erupts in the Geonosian arena as the Grand Army of the Republic is unveiled against the Separatist droid armies.",
            CanonType.CanonAndLegends,
            -22,
            "22 BBY",
            2,
            ["Anakin Skywalker", "Obi-Wan Kenobi", "Padme Amidala", "Yoda", "Count Dooku"],
            ["Geonosis", "Petranaki arena"],
            ["LAAT/i gunship", "Acclamator-class assault ship", "Droid control ship"]),
        new(
            "The Siege of Mandalore",
            "Ahsoka Tano and Bo-Katan Kryze lead a Clone army in a climactic assault on Mandalore to capture Darth Maul as the Clone Wars draw to a close.",
            CanonType.Canon,
            -19,
            "19 BBY",
            10,
            ["Ahsoka Tano", "Bo-Katan Kryze", "Darth Maul"],
            ["Mandalore", "Sundari"],
            ["The Tribunal", "LAAT/i gunship", "Gauntlet fighter"]),
        new(
            "Order 66",
            "Palpatine activates the Clone army contingency to exterminate the Jedi across the galaxy, extinguishing the order in a single sweeping betrayal.",
            CanonType.CanonAndLegends,
            -19,
            "19 BBY",
            3,
            ["Emperor Palpatine", "Anakin Skywalker", "Mace Windu", "The Jedi Order"],
            ["Coruscant", "Utapau", "Mygeeto", "Felucia", "Kashyyyk"],
            ["Venator-class Star Destroyer", "LAAT/i gunship"]),
        new(
            "The Destruction of Alderaan",
            "The Death Star obliterates Alderaan as a demonstration of Imperial power, galvanizing the galaxy against the Empire.",
            CanonType.CanonAndLegends,
            0,
            "0 BBY",
            4,
            ["Princess Leia Organa", "Grand Moff Tarkin"],
            ["Alderaan", "Alderaan system"],
            ["Death Star", "Tantive IV"]),
        new(
            "The Battle of Yavin",
            "Rebel pilots, led by Luke Skywalker, launch a desperate trench run against the Death Star, destroying the superweapon and igniting a galaxy-wide rebellion.",
            CanonType.CanonAndLegends,
            0,
            "0 BBY",
            4,
            ["Luke Skywalker", "Han Solo", "Princess Leia Organa", "Darth Vader"],
            ["Yavin Prime", "Yavin 4"],
            ["Millennium Falcon", "Death Star", "T-65 X-wing starfighter", "TIE/LN fighter"]),
        new(
            "The Battle of Hoth",
            "The Empire storms the Rebel base on Hoth in a massive ground assault, forcing the Alliance to scatter across the galaxy.",
            CanonType.CanonAndLegends,
            3,
            "3 ABY",
            5,
            ["Luke Skywalker", "Han Solo", "Princess Leia Organa", "Darth Vader"],
            ["Hoth", "Echo Base"],
            ["AT-AT walker", "T-47 snowspeeder", "Executor"]),
        new(
            "The Battle of Endor",
            "The Rebel Alliance destroys the second Death Star and defeats the Imperial fleet over Endor, sealing the Empire fate and the fall of Emperor Palpatine.",
            CanonType.CanonAndLegends,
            4,
            "4 ABY",
            6,
            ["Luke Skywalker", "Han Solo", "Princess Leia Organa", "Darth Vader", "Emperor Palpatine"],
            ["Endor", "Death Star II"],
            ["Millennium Falcon", "Death Star II", "Executor", "A-wing starfighter"]),
        new(
            "The Rescue",
            "Din Djarin and his allies board Moff Gideon cruiser to rescue Grogu, defeating the Imperial warlord and reuniting the Mandalorian with his foundling.",
            CanonType.Canon,
            9,
            "9 ABY",
            12,
            ["Din Djarin", "Grogu", "Bo-Katan Kryze", "Moff Gideon"],
            ["Nevarro", "Gideon light cruiser"],
            ["Razor Crest", "Arquitens-class light cruiser"]),
        new(
            "The Battle of Exegol",
            "The Resistance strikes at the hidden Sith world of Exegol, where the resurrected Palpatine commands the Final Order fleet of Star Destroyers.",
            CanonType.Canon,
            35,
            "35 ABY",
            9,
            ["Rey", "Ben Solo", "Emperor Palpatine", "Lando Calrissian", "Poe Dameron"],
            ["Exegol", "Kijimi"],
            ["T-70 X-wing starfighter", "TIE whisper", "Xyston-class Star Destroyer"]),
        new(
            "The Second Galactic Civil War",
            "Jacen Solo falls to the dark side and seizes control of the Galactic Alliance, plunging the galaxy into a bitter civil war that divides the Jedi.",
            CanonType.Legends,
            40,
            "40 ABY",
            20,
            ["Jacen Solo", "Jaina Solo", "Luke Skywalker", "Mara Jade Skywalker"],
            ["Coruscant", "Kashyyyk"],
            ["Imperial-class Star Destroyer", "StealthX starfighter"])
    ];
}

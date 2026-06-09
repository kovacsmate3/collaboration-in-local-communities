using Backend.Domain.Enums;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Curated, deterministic demo dataset for the Budapest marketplace: a fixed
/// set of sample seeker accounts and 50 realistic community tasks spread across
/// every category, compensation mode, and lifecycle status.
/// </summary>
/// <remarks>
/// Pure data with no behaviour — the <see cref="SampleTaskSeeder"/> owns the
/// idempotent persistence logic. Keeping the catalogue here (SRP) lets the
/// dataset grow or be tuned without touching the seeding mechanics.
/// </remarks>
internal static class BudapestSampleData
{
    /// <summary>
    /// Shared password for every seeded sample account. Local-development only;
    /// these accounts are never created outside Development.
    /// </summary>
    public const string SeekerPassword = "Seed123!";

    /// <summary>
    /// <c>PublicCode</c> prefix marking a task as part of this demo dataset.
    /// Used as the idempotency key so re-running the seeder never duplicates.
    /// </summary>
    public const string TaskCodePrefix = "DEMO-";

    /// <summary>Gets the sample seeker accounts that own the demo tasks.</summary>
    public static IReadOnlyList<SampleSeeker> Seekers { get; } =
    [
        new("seed.anna@local.test", "Anna K.", "XI. kerület", "Újbudai lakos, szívesen segítek és kérek segítséget a környéken."),
        new("seed.bence@local.test", "Bence T.", "VII. kerület", "Belvárosi srác, sokat biciklizem a városban."),
        new("seed.csilla@local.test", "Csilla M.", "XIII. kerület", "Két gyerek mellett mindig jól jön egy kis kéz a környékről."),
        new("seed.daniel@local.test", "Dániel P.", "II. kerület", "Budai oldalon élek, barkácsolni is szeretek."),
        new("seed.eszter@local.test", "Eszter V.", "IX. kerület", "Ferencvárosi egyetemista, rugalmas időbeosztással."),
        new("seed.gabor@local.test", "Gábor Sz.", "XIV. kerület", "Zuglói családapa, hétvégente érek rá igazán."),
        new("seed.hanna@local.test", "Hanna B.", "VI. kerület", "Terézvárosban lakom, imádom a közösségi dolgokat."),
        new("seed.istvan@local.test", "István L.", "III. kerület", "Óbudai nyugdíjas, sok mindenhez értek még."),
        new("seed.julia@local.test", "Júlia N.", "V. kerület", "Belvárosi lakó, gyakran van szükségem apró segítségre."),
        new("seed.mark@local.test", "Márk H.", "VIII. kerület", "Józsefvárosi fiatal, megbízható és pontos."),
    ];

    /// <summary>
    /// Gets the 50 demo tasks. Order is significant: the task's stable
    /// <c>PublicCode</c> is derived from its 1-based position, so entries must
    /// only ever be appended, never reordered, to keep seeding idempotent.
    /// </summary>
    public static IReadOnlyList<SampleTask> Tasks { get; } =
    [
        new("Költözés: dobozok cipelése", "Néhány doboz és egy kanapé levitele a harmadik emeletről, lift nincs. Két erős kéz kellene szombat délelőttre.", "moving", CompensationType.Paid, 8000m, "XI. kerület, Bartók Béla út", 47.4760, 19.0400, DomainTaskStatus.Open),
        new("Bútorszállítás kisteherautóval", "Egy szekrényt vinnék át Zuglóból Óbudára. Ha van kisbuszod, megegyezünk.", "moving", CompensationType.Paid, 15000m, "XIV. kerület, Bosnyák tér", 47.5210, 19.1080, DomainTaskStatus.InProgress),
        new("Zongora mozgatása a lakáson belül", "Egy pianínót kellene áttolni a másik szobába, egyedül nem megy. Negyed óra az egész.", "moving", CompensationType.Voluntary, null, "II. kerület, Margit körút", 47.5120, 19.0290, DomainTaskStatus.Open),
        new("Albérletbe költözés segítség", "Diákként költözöm, pár bőrönd és könyvesdoboz. Sörrel és pizzával hálálom meg.", "moving", CompensationType.Barter, null, "IX. kerület, Ferenc körút", 47.4830, 19.0680, DomainTaskStatus.Open),
        new("Mosógép leszerelése és elszállítása", "Régi mosógépet kéne kivinni a lépcsőházból a konténerhez. Nehéz darab.", "moving", CompensationType.Paid, 6000m, "XIII. kerület, Lehel tér", 47.5340, 19.0700, DomainTaskStatus.Completed),
        new("Hűtő felvitele a 4. emeletre", "Új hűtőt hoztak, de a futár csak a kapuig hozta. Lift van, de szűk.", "moving", CompensationType.Points, null, "VI. kerület, Nagymező utca", 47.5050, 19.0610, DomainTaskStatus.Open),
        new("Lomtalanításra pakolás", "Pincét ürítek, sok a régi cucc. Aki segít cipelni, az vihet is belőle, ami tetszik.", "moving", CompensationType.Barter, null, "III. kerület, Flórián tér", 47.5680, 19.0410, DomainTaskStatus.Open),

        new("Matek korrepetálás 8. osztályosnak", "Felvételire készülünk, heti két alkalom kellene. Türelmes tanárt keresek.", "tutoring", CompensationType.Paid, 5000m, "V. kerület, Deák Ferenc tér", 47.4979, 19.0513, DomainTaskStatus.Open),
        new("Angol társalgási gyakorlás", "Középfokom megvan, csak a beszéd megy nehezen. Heti egy óra kötetlen beszélgetés.", "tutoring", CompensationType.Paid, 4000m, "VII. kerület, Wesselényi utca", 47.5000, 19.0700, DomainTaskStatus.InProgress),
        new("Programozás alapok – Python", "Pályamódosító vagyok, a legelején tartok. Valaki, aki érthetően el tudja magyarázni.", "tutoring", CompensationType.Paid, 7000m, "XIII. kerület, Váci út", 47.5380, 19.0680, DomainTaskStatus.Open),
        new("Gitároktatás kezdőnek", "Most kezdtem, pár akkordot szeretnék megtanulni. Cserébe szívesen sütök neked.", "tutoring", CompensationType.Barter, null, "XIV. kerület, Thököly út", 47.5150, 19.0900, DomainTaskStatus.Open),
        new("Német házi feladat segítség", "Gimnazista lányomnak kellene rendszeres segítség németből. Hétköznap délután.", "tutoring", CompensationType.Paid, 4500m, "II. kerület, Széll Kálmán tér", 47.5070, 19.0250, DomainTaskStatus.Open),
        new("Fizika érettségire felkészítés", "Emelt szintű fizikára készülök, sok a hiányosság. Tapasztalt korrepetálót keresek.", "tutoring", CompensationType.Paid, 6000m, "XI. kerület, Móricz Zsigmond körtér", 47.4770, 19.0450, DomainTaskStatus.PendingApproval),
        new("Számítógép-használat idős szomszédnak", "Édesanyámat tanítaná valaki türelmesen e-mailezni, videóhívni. Önkéntes alapon hálás lennék.", "tutoring", CompensationType.Voluntary, null, "IV. kerület, Újpest-központ", 47.5640, 19.0900, DomainTaskStatus.Open),

        new("Csöpögő csap megjavítása", "A konyhai csap folyamatosan csöpög, biztos csak egy tömítés. Aki ért hozzá, jöjjön.", "repairs", CompensationType.Paid, 5000m, "VIII. kerület, Baross utca", 47.4870, 19.0750, DomainTaskStatus.Open),
        new("Polc felfúrása a falra", "Két polcot szeretnék a nappaliba, a tipli is megvan. Fúró kell hozzá.", "repairs", CompensationType.Paid, 4000m, "VI. kerület, Andrássy út", 47.5070, 19.0660, DomainTaskStatus.Completed),
        new("Konnektor cseréje", "Egy konnektor kilazult, biztonságosan kicserélném. Aki konyít az elektromossághoz.", "repairs", CompensationType.Points, null, "IX. kerület, Üllői út", 47.4760, 19.0820, DomainTaskStatus.Open),
        new("Kerékpár fékállítás", "A hátsó fék gyengén fog, nem merek vele tekerni. Cserébe megjavítom a gépedet, ha kell.", "repairs", CompensationType.Barter, null, "XIII. kerület, Pozsonyi út", 47.5260, 19.0540, DomainTaskStatus.Open),
        new("Beázott parketta – szakértő szem", "Kis folt a parkettán, nem tudom, komoly-e. Valaki, aki ránézne és tanácsot adna.", "repairs", CompensationType.Voluntary, null, "II. kerület, Pasarét", 47.5160, 19.0150, DomainTaskStatus.Open),
        new("Redőny javítása – elakadt", "A hálószobai redőny félúton megállt, nem megy se fel, se le.", "repairs", CompensationType.Paid, 6500m, "XIV. kerület, Zugló", 47.5210, 19.1080, DomainTaskStatus.InProgress),

        new("Heti bevásárlás idős néninek", "A szomszéd néni nehezen mozog, hetente egyszer behoznám neki a boltból az alapokat.", "shopping", CompensationType.Voluntary, null, "VII. kerület, Klauzál tér", 47.4980, 19.0660, DomainTaskStatus.Open),
        new("IKEA-túra autóval", "Pár bútort vennék az IKEA-ban, de nincs autóm. Aki elvinne és segítene behozni.", "shopping", CompensationType.Paid, 7000m, "XIX. kerület, Kispest", 47.4560, 19.1450, DomainTaskStatus.Open),
        new("Gyógyszer kiváltása a patikában", "Lázas vagyok, nem tudok kimozdulni. Valaki kiváltaná a felírt gyógyszert?", "shopping", CompensationType.Points, null, "V. kerület, Bálvány utca", 47.5000, 19.0500, DomainTaskStatus.Completed),
        new("Piaci bevásárlás hétvégén", "Vásárcsarnokba mennék zöldségért, de nehéz cipelni. Segítség a cipekedésben.", "shopping", CompensationType.Barter, null, "IX. kerület, Fővám tér", 47.4870, 19.0590, DomainTaskStatus.Open),
        new("Karácsonyi ajándékok beszerzése", "Nincs időm végigjárni a boltokat, a lista megvan. Megbízható segítőt keresek.", "shopping", CompensationType.Paid, 5000m, "VI. kerület, WestEnd", 47.5110, 19.0570, DomainTaskStatus.Open),
        new("Nagybevásárlás – cipelés", "Havi nagybevásárlás a Sparban, sok a nehéz cucc. Egy óra, jó hangulatban.", "shopping", CompensationType.Points, null, "XI. kerület, Allee", 47.4760, 19.0490, DomainTaskStatus.Open),

        new("Kutyasétáltatás munkanapokon", "Hétköznap délben nem érek haza, a kutyusom kimaradna. Napi egy séta kellene.", "pet_care", CompensationType.Paid, 3000m, "XIII. kerület, Szent István park", 47.5300, 19.0500, DomainTaskStatus.Open),
        new("Macskafelügyelet hétvégére", "Elutazunk két napra, a cicát etetni kéne és kicsit foglalkozni vele.", "pet_care", CompensationType.Paid, 8000m, "II. kerület, Rózsadomb", 47.5230, 19.0230, DomainTaskStatus.InProgress),
        new("Kutyakozmetikába szállítás", "A kutyám fél a busztól, autóval kéne elvinni a kozmetikushoz és vissza.", "pet_care", CompensationType.Points, null, "XIV. kerület, Bosnyák tér", 47.5210, 19.1080, DomainTaskStatus.Open),
        new("Akvárium gondozás nyaralás alatt", "Egy hétre elmegyünk, az akváriumot etetni és figyelni kéne. Megmutatom a teendőket.", "pet_care", CompensationType.Voluntary, null, "XI. kerület, Gazdagrét", 47.4660, 19.0150, DomainTaskStatus.Open),
        new("Papagáj-felügyelet", "Hosszú hétvégére keresek valakit, aki ránéz a papagájomra és tölt neki vizet.", "pet_care", CompensationType.Barter, null, "VII. kerület, Almássy tér", 47.5010, 19.0720, DomainTaskStatus.Open),
        new("Idős kutya esti séta", "A kutyusunk már lassan jár, csak egy nyugodt esti sétára van szüksége.", "pet_care", CompensationType.Points, null, "III. kerület, Békásmegyer", 47.5950, 19.0500, DomainTaskStatus.Cancelled),

        new("Nagytakarítás költözés után", "Kiköltöztünk, az üres lakást kéne alaposan kitakarítani átadás előtt.", "cleaning", CompensationType.Paid, 12000m, "VIII. kerület, Corvin-negyed", 47.4860, 19.0720, DomainTaskStatus.Open),
        new("Ablaktisztítás tavaszra", "Négy nagy ablak a nappaliban, kívül-belül. Akinek a magasban dolgozás nem ijesztő.", "cleaning", CompensationType.Paid, 6000m, "XIII. kerület, Angyalföld", 47.5400, 19.0780, DomainTaskStatus.Open),
        new("Erkély rendrakás", "Az erkély tele van télen felgyűlt kacattal, kéne két kéz a pakoláshoz.", "cleaning", CompensationType.Barter, null, "VI. kerület, Oktogon", 47.5060, 19.0640, DomainTaskStatus.Completed),
        new("Konyha alapos sikálás", "Zsíros lett a konyha a sok főzéstől, kéne egy alapos nagytakarítás.", "cleaning", CompensationType.Paid, 7000m, "IX. kerület, Boráros tér", 47.4730, 19.0660, DomainTaskStatus.Open),
        new("Garázs kipakolás és söprés", "Évek óta gyűlik a garázsban a lom, kéne segítség a kipakoláshoz.", "cleaning", CompensationType.Points, null, "II. kerület, Hűvösvölgy", 47.5300, 18.9900, DomainTaskStatus.Open),
        new("Lépcsőház nagytakarítás", "Társasházban közösen takarítanánk a lépcsőházat, jó csapatban. Önkéntes alapon.", "cleaning", CompensationType.Voluntary, null, "VII. kerület, Rottenbiller utca", 47.5070, 19.0760, DomainTaskStatus.Open),

        new("Csomag feladása a postán", "Két csomagot kéne feladni, de munkaidőben nem érek oda. Kis ügyintézés.", "errands", CompensationType.Points, null, "V. kerület, Október 6. utca", 47.5000, 19.0510, DomainTaskStatus.Open),
        new("Sorban állás az okmányirodában", "Hosszú a sor az okmányirodában, valaki beülne helyettem, amíg dolgozom?", "errands", CompensationType.Paid, 4000m, "XIII. kerület, Visegrádi utca", 47.5300, 19.0600, DomainTaskStatus.Open),
        new("Növények locsolása nyaralás alatt", "Egy hétre elutazom, a sok szobanövényt kéne meglocsolni kétnaponta.", "errands", CompensationType.Voluntary, null, "XI. kerület, Lágymányos", 47.4720, 19.0560, DomainTaskStatus.Open),
        new("Bútor házhozszállítás átvétele", "Szállításra várok, de nem tudok otthon lenni. Valaki átvenné helyettem.", "errands", CompensationType.Barter, null, "XIV. kerület, Füredi utca", 47.5170, 19.1180, DomainTaskStatus.Open),
        new("Kulcs leadása a szomszédnál", "A takarítónak kéne kulcsot adni, de elcsúsztam időben. Pár perc az egész.", "errands", CompensationType.Points, null, "VI. kerület, Teréz körút", 47.5080, 19.0620, DomainTaskStatus.Completed),
        new("Biciklis futár rövid távra", "Egy fontos iratot kéne átvinni a belvárosba még ma. Aki gyorsan tekerne.", "errands", CompensationType.Paid, 3500m, "VII. kerület, Károly körút", 47.4960, 19.0590, DomainTaskStatus.InProgress),

        new("Fényképezés egy családi eseményre", "Kis kerti összejövetel lesz, valaki, aki kapna pár jó képet rólunk. Telefonnal is jó.", "other", CompensationType.Paid, 10000m, "XII. kerület, Normafa", 47.4970, 18.9900, DomainTaskStatus.Open),
        new("Számítógép összerakása alkatrészekből", "Megvettem a részeket egy géphez, de nem merek nekiállni. Aki ért hozzá.", "other", CompensationType.Paid, 8000m, "XIII. kerület, Dózsa György út", 47.5280, 19.0750, DomainTaskStatus.Open),
        new("Önéletrajz átnézése, tanács", "Először keresek munkát, kéne egy szem, aki átnézi a CV-met és tanácsot ad.", "other", CompensationType.Voluntary, null, "IX. kerület, Corvin", 47.4850, 19.0710, DomainTaskStatus.Open),
        new("Kerti gázgrill beüzemelése", "Új gázgrillt vettünk, de nem merjük bekötni. Aki ért a gázpalackhoz.", "other", CompensationType.Points, null, "XVI. kerület, Sashalom", 47.5180, 19.1900, DomainTaskStatus.Open),
        new("Társasjáték-est szervezése", "Keresek lelkes embereket egy havi társasjáték-estre a környéken. Jó hangulat garantált.", "other", CompensationType.Voluntary, null, "VIII. kerület, Palotanegyed", 47.4900, 19.0680, DomainTaskStatus.Open),
        new("Régi fotók digitalizálása", "Egy doboznyi papírkép, amit szkennelni kéne. Cserébe főzök egy jó ebédet.", "other", CompensationType.Barter, null, "II. kerület, Marczibányi tér", 47.5130, 19.0220, DomainTaskStatus.Open),
    ];
}

/// <summary>One seeded sample seeker account.</summary>
internal sealed record SampleSeeker(
    string Email,
    string DisplayName,
    string District,
    string Bio);

/// <summary>One curated demo task template.</summary>
internal sealed record SampleTask(
    string Title,
    string Description,
    string CategoryCode,
    CompensationType CompensationType,
    decimal? Amount,
    string LocationText,
    double Latitude,
    double Longitude,
    DomainTaskStatus Status);

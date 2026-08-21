using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Business.Seeding;

/// <summary>
/// The pseudonymised speeding case from <c>test-data/case-01-speeding.md</c>, transcribed by hand
/// (SDD-017). Sub-case acts are invented — the source only records their counts.
/// </summary>
internal static class SampleData
{
    public const string SubjectKey = "subject";

    public const string MainCaseKey = "main";

    public static IReadOnlyList<SampleContact> Contacts { get; } =
    [
        new()
        {
            Key = SubjectKey,
            Kind = ContactKind.Person,
            Name = "Ing. Petr Vzorek",
            Address = "Vzorová 1, 100 00 Vzorov",
            DataBoxId = "abc1def",
        },
        new()
        {
            Key = "first-instance",
            Kind = ContactKind.Authority,
            Name = "Městský úřad Vzorov, odbor vnitřních věcí",
            Address = "náměstí Míru 1, 100 00 Vzorov",
            DataBoxId = "vzorov1",
        },
        new()
        {
            Key = "appellate",
            Kind = ContactKind.Authority,
            Name = "Krajský úřad Vzorového kraje",
            Address = "Krajská 2, 101 00 Vzorov",
            DataBoxId = "kuvz123",
        },
        new()
        {
            Key = "court",
            Kind = ContactKind.Authority,
            Name = "Krajský soud ve Vzorově",
            Address = "Soudní 3, 101 00 Vzorov",
            DataBoxId = "ksvz456",
        },
        new()
        {
            Key = "police",
            Kind = ContactKind.Authority,
            Name = "Policie Vzorového kraje",
            Address = "Policejní 4, 100 00 Vzorov",
            DataBoxId = "polvz78",
        },
        new()
        {
            Key = "ministry-transport",
            Kind = ContactKind.Authority,
            Name = "Ministerstvo dopravy",
            Address = "Dopravní 5, 110 00 Praha",
            DataBoxId = "mindop1",
        },
        new()
        {
            Key = "ministry-interior",
            Kind = ContactKind.Authority,
            Name = "Ministerstvo vnitra",
            Address = "Vnitřní 6, 110 00 Praha",
            DataBoxId = "minvni1",
        },
        new()
        {
            Key = "roads",
            Kind = ContactKind.Authority,
            Name = "Ředitelství silnic a dálnic",
            Address = "Silniční 7, 110 00 Praha",
            DataBoxId = "rsdcr12",
        },
        new()
        {
            Key = "bar",
            Kind = ContactKind.Authority,
            Name = "Česká advokátní komora",
            Address = "Advokátní 8, 110 00 Praha",
            DataBoxId = "cakcz12",
        },
        new()
        {
            Key = "insurer",
            Kind = ContactKind.Authority,
            Name = "Vzorová pojišťovna, a.s.",
            Address = "Pojišťovací 9, 110 00 Praha",
            DataBoxId = "pojvz12",
        },
        new()
        {
            Key = "mayor",
            Kind = ContactKind.Official,
            Name = "starosta Městského úřadu Vzorov",
        },
        new()
        {
            Key = "officer",
            Kind = ContactKind.Official,
            Name = "pověřená úřední osoba Městského úřadu Vzorov",
        },
    ];

    public static IReadOnlyList<SampleCase> Cases { get; } =
    [
        new()
        {
            Key = MainCaseKey,
            Title = "Překročení rychlosti Vzorov — 121 km/h v úseku 110",
            Status = CaseStatus.Active,
            Date = new DateOnly(2025, 5, 28),
            Description = "Řízení o odpovědnosti provozovatele vozidla za překročení nejvyšší dovolené "
                + "rychlosti. Vede od příkazu přes prvostupňové rozhodnutí a odvolání až k žalobě u "
                + "krajského soudu.",
            ExternalNumbers =
            [
                new() { Value = "VV41/2025/08464", AssignedByKey = "first-instance" },
                new() { Value = "10 A 1/2025", AssignedByKey = "court" },
            ],
            Comments =
            [
                "Pseudonymizovaná vzorová data z reálného spisu. Nic v nich není skutečné.",
                "Podřízené spisy jsou převážně žádosti o informace, kterými se sbírají podklady pro hlavní linii.",
            ],
        },
        new()
        {
            Key = "s01",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Ministerstvo dopravy",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 2),
            CounterpartyKey = "ministry-transport",
            Description = "Podklady k měřicímu zařízení a smlouvě o jeho provozu.",
        },
        new()
        {
            Key = "s02",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Policie Vzorového kraje",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 3),
            CounterpartyKey = "police",
            Description = "Podklady k provedenému měření rychlosti.",
        },
        new()
        {
            Key = "s03",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 1",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 4),
            CounterpartyKey = "first-instance",
            Description = "První žádost o informace k vedenému řízení.",
        },
        new()
        {
            Key = "s04",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 2, sběrný arch spisu",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 5),
            CounterpartyKey = "first-instance",
            Description = "Žádost o sběrný arch spisu.",
        },
        new()
        {
            Key = "s05",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 3",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 6),
            CounterpartyKey = "first-instance",
            Description = "Žádost doprovázená stížností na nečinnost.",
        },
        new()
        {
            Key = "s06",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 4, přípis",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 9),
            CounterpartyKey = "first-instance",
            Description = "Žádost o přípis založený ve spisu.",
        },
        new()
        {
            Key = "s07",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 4, přípis krajskému úřadu",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 10),
            CounterpartyKey = "appellate",
            Description = "Táž žádost směřovaná ke krajskému úřadu.",
        },
        new()
        {
            Key = "s08",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — Vzorov 5, pokyny pověřené úřední osobě",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 11),
            CounterpartyKey = "first-instance",
            Description = "Žádost o pokyny udělené pověřené úřední osobě.",
        },
        new()
        {
            Key = "s09",
            ParentKey = MainCaseKey,
            Title = "Žádost o informace — silniční správní úřad",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 6, 12),
            CounterpartyKey = "roads",
            Description = "Žádost o podklady k dopravnímu značení v úseku měření.",
        },
        new()
        {
            Key = "s10",
            ParentKey = MainCaseKey,
            Title = "Předžalobní výzva",
            Status = CaseStatus.Active,
            Date = new DateOnly(2025, 8, 20),
            CounterpartyKey = "first-instance",
            Description = "Výzva k nápravě před podáním žaloby.",
        },
        new()
        {
            Key = "s11",
            ParentKey = MainCaseKey,
            Title = "Otevřený dopis zastupitelstvu",
            Status = CaseStatus.Active,
            Date = new DateOnly(2025, 8, 25),
            CounterpartyKey = "first-instance",
            Description = "Otevřený dopis k postupu úřadu.",
        },
        new()
        {
            Key = "s12",
            ParentKey = MainCaseKey,
            Title = "Náhrada nemajetkové újmy — Ministerstvo vnitra",
            Status = CaseStatus.WaitingOnAuthority,
            Date = new DateOnly(2025, 9, 10),
            CounterpartyKey = "ministry-interior",
            Description = "Uplatnění nároku na náhradu nemajetkové újmy.",
        },
        new()
        {
            Key = "s13",
            ParentKey = "s12",
            Title = "Žádost o informace — Ministerstvo vnitra",
            Status = CaseStatus.Closed,
            Date = new DateOnly(2025, 9, 15),
            CounterpartyKey = "ministry-interior",
            Description = "Žádost o informace vedená uvnitř nároku na náhradu újmy.",
        },
        new()
        {
            Key = "s14",
            ParentKey = MainCaseKey,
            Title = "Oznámení přestupku starosty",
            Status = CaseStatus.WaitingOnAuthority,
            Date = new DateOnly(2025, 9, 20),
            CounterpartyKey = "first-instance",
            Description = "Oznámení přestupku starosty a navazující přezkum.",
            Comments = ["Přestupkové řízení proti starostovi běží samostatně."],
        },
        new()
        {
            Key = "s15",
            ParentKey = MainCaseKey,
            Title = "Stížnost k České advokátní komoře",
            Status = CaseStatus.Active,
            Date = new DateOnly(2025, 9, 25),
            CounterpartyKey = "bar",
            Description = "Kárný podnět k České advokátní komoře.",
        },
        new()
        {
            Key = "s16",
            ParentKey = MainCaseKey,
            Title = "Uplatnění pojištění právní ochrany",
            Status = CaseStatus.Active,
            Date = new DateOnly(2025, 10, 2),
            CounterpartyKey = "insurer",
            Description = "Oznámení škodné události pojišťovně.",
        },
    ];

    public static IReadOnlyList<SampleAct> MainCaseActs { get; } =
    [
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Výzva k úhradě určené částky",
            Date = new DateOnly(2025, 7, 15),
            CounterpartyKey = "first-instance",
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Příkaz o uložení pokuty",
            Date = new DateOnly(2025, 7, 31),
            CounterpartyKey = "first-instance",
            Description = "Pokuta 2 000 Kč, náklady řízení 2 500 Kč.",
            ExternalNumbers = [new() { Value = "MUVZ/2025/80535", AssignedByKey = "first-instance" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Odpor proti příkazu",
            Date = new DateOnly(2025, 8, 4),
            CounterpartyKey = "first-instance",
            Description = "Odpor ruší příkaz v plném rozsahu.",
            ExternalNumbers = [new() { Value = "VV41/2025/08464", AssignedByKey = "first-instance" }],
            Comments = ["Odpor ruší příkaz v plném rozsahu, řízení pokračuje."],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Vyrozumění o pokračování řízení a výzva k vyjádření se k podkladům",
            Date = new DateOnly(2025, 8, 6),
            CounterpartyKey = "first-instance",
            ExternalNumbers = [new() { Value = "MUVZ/2025/82743", AssignedByKey = "first-instance" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Vyjádření k podkladům rozhodnutí",
            Date = new DateOnly(2025, 8, 18),
            CounterpartyKey = "first-instance",
            ExternalNumbers = [new() { Value = "VV41/2025/08464", AssignedByKey = "first-instance" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Rozhodnutí o přestupku v prvním stupni",
            Date = new DateOnly(2025, 9, 2),
            CounterpartyKey = "first-instance",
            Description = "Vina, pokuta 2 000 Kč, náklady řízení 2 500 Kč.",
            ExternalNumbers = [new() { Value = "MUVZ/2025/93547", AssignedByKey = "first-instance" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Odvolání proti rozhodnutí v plném rozsahu",
            Date = new DateOnly(2025, 9, 15),
            CounterpartyKey = "first-instance",
            ExternalNumbers = [new() { Value = "MUVZ/2025/93547", AssignedByKey = "first-instance" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Rozhodnutí o odvolání",
            Date = new DateOnly(2025, 9, 26),
            CounterpartyKey = "appellate",
            Description = "Odvolání zamítnuto, rozhodnutí prvního stupně potvrzeno.",
            ExternalNumbers = [new() { Value = "KUVZ 109838/2025", AssignedByKey = "appellate" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Žaloba proti rozhodnutí správního orgánu s návrhem na přiznání odkladného účinku",
            Date = new DateOnly(2025, 10, 1),
            CounterpartyKey = "court",
            Description = "Žaloba směřuje proti rozhodnutí o odvolání.",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
            Comments = ["Součástí žaloby je návrh na přiznání odkladného účinku."],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Poučení o možné podjatosti senátu",
            Date = new DateOnly(2025, 10, 6),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Vyjádření k poučení, souhlas s rozhodnutím bez jednání a vyčíslení nákladů",
            Date = new DateOnly(2025, 10, 7),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Výzva k zaplacení soudního poplatku",
            Date = new DateOnly(2025, 10, 9),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025-31", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Doplnění žaloby — materiální stránka přestupku",
            Date = new DateOnly(2025, 10, 13),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Doplnění žaloby — nezákonnost měření rychlosti",
            Date = new DateOnly(2025, 10, 14),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Usnesení o nepřiznání odkladného účinku",
            Date = new DateOnly(2025, 10, 15),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Přípis soudu",
            Date = new DateOnly(2025, 10, 16),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Incoming,
            Title = "Vyjádření žalovaného k žalobě",
            Date = new DateOnly(2025, 10, 17),
            CounterpartyKey = "appellate",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Vyčíslení nákladů řízení",
            Date = new DateOnly(2025, 10, 18),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Doplnění žaloby — rozpor mezi rozhodnutími a replika k vyjádření žalovaného",
            Date = new DateOnly(2025, 11, 3),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Žádost o prodloužení lhůty k doplnění žaloby",
            Date = new DateOnly(2025, 11, 10),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Druhá žádost o prodloužení lhůty",
            Date = new DateOnly(2025, 11, 25),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            // The source document says 2026-12-30, after the final supplement; seeded a year earlier so
            // the act list stays in date order.
            Direction = ActDirection.Outgoing,
            Title = "Třetí žádost o prodloužení lhůty",
            Date = new DateOnly(2025, 12, 30),
            CounterpartyKey = "court",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
        },
        new()
        {
            Direction = ActDirection.Outgoing,
            Title = "Konečné doplnění žaloby",
            Date = new DateOnly(2026, 3, 13),
            CounterpartyKey = "court",
            Description = "Konečné doplnění s přiloženou sadou důkazů.",
            ExternalNumbers = [new() { Value = "10 A 1/2025", AssignedByKey = "court" }],
            Comments = ["Přílohou je sada důkazů shromážděná v podřízených spisech."],
            ExtraFileSuffix = "prilohy",
        },
    ];
}

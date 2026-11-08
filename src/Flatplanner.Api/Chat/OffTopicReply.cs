namespace Flatplanner.Api.Chat;

public static class OffTopicReply
{
    public const string NoticeCode = "off_topic";

    private const string Lead = "Nie rozumiem tego pytania, bo nie dotyczy mieszkań. Za to mogę opowiedzieć dowcip:";

    private static readonly string[] Jokes =
    [
        "Przychodzi baba do lekarza: „Panie doktorze, wszystko mnie boli. Dotknę głowy, boli. Dotknę brzucha, boli. Dotknę nogi, boli”. Lekarz ją bada i mówi: „Ma pani złamany palec”.",
        "Przychodzi baba do lekarza: „Panie doktorze, chyba tracę pamięć”. Lekarz: „Od kiedy?”. Baba: „Od czego?”.",
        "Przychodzi baba do lekarza: „Panie doktorze, sąsiad wierci od ósmej rano”. Lekarz: „To nie moja specjalność, ale polecam ostatnie piętro i grube ściany”.",
        "Sprzedawca: „To mieszkanie ma duży potencjał”. Kupujący: „Czyli remont zrobię sam?”. Sprzedawca: „Widzi pan, jak szybko się rozumiemy”.",
        "Wchodzi facet do biura nieruchomości: „Poproszę mieszkanie ciche, tanie i w centrum”. Agent: „Mogę zaproponować jedno z trzech”.",
    ];

    public static string Compose(int index) => $"{Lead} {Jokes[index % Jokes.Length]}";

    public static string Pick() => Compose(Random.Shared.Next(Jokes.Length));
}

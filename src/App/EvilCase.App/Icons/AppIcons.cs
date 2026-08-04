using TabBlazor;

namespace EvilBrains.EvilCase.App.Icons;

// TabBlazor ships no icon set. Path data comes from the Tabler icon set; add icons here as they are needed.
public static class AppIcons
{
    public static IIconType AlertTriangle { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M12 9v4' />"
        + "<path d='M10.363 3.591l-8.106 13.534a1.914 1.914 0 0 0 1.636 2.871h16.214a1.914 1.914 0 0 0 1.636 -2.87l-8.106 -13.536a1.914 1.914 0 0 0 -3.274 0z' />"
        + "<path d='M12 16h.01' />");

    public static IIconType ArrowsExchange { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M7 10h14l-4 -4' />"
        + "<path d='M17 14h-14l4 4' />");

    public static IIconType Clock { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0' />"
        + "<path d='M12 7v5l3 3' />");

    public static IIconType FileText { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M14 3v4a1 1 0 0 0 1 1h4' />"
        + "<path d='M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2z' />"
        + "<path d='M9 9l1 0' />"
        + "<path d='M9 13l6 0' />"
        + "<path d='M9 17l6 0' />");

    public static IIconType Folders { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M9 4h3l2 2h5a2 2 0 0 1 2 2v7a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2v-9a2 2 0 0 1 2 -2' />"
        + "<path d='M17 17v2a2 2 0 0 1 -2 2h-10a2 2 0 0 1 -2 -2v-9a2 2 0 0 1 2 -2h2' />");

    public static IIconType LayoutDashboard { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M5 4h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-6a1 1 0 0 1 1 -1' />"
        + "<path d='M5 16h4a1 1 0 0 1 1 1v2a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-2a1 1 0 0 1 1 -1' />"
        + "<path d='M15 12h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-6a1 1 0 0 1 1 -1' />"
        + "<path d='M15 4h4a1 1 0 0 1 1 1v2a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-2a1 1 0 0 1 1 -1' />");

    public static IIconType Moon { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M12 3c.132 0 .263 0 .393 0a7.5 7.5 0 0 0 7.92 12.446a9 9 0 1 1 -8.313 -12.454z' />");

    public static IIconType Scale { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M7 20l10 0' />"
        + "<path d='M6 6l6 -1l6 1' />"
        + "<path d='M12 3l0 17' />"
        + "<path d='M9 12l-3 -6l-3 6a3 3 0 0 0 6 0' />"
        + "<path d='M21 12l-3 -6l-3 6a3 3 0 0 0 6 0' />");

    // The first path exceeds the line limit, so it is split at SVG command boundaries.
    public static IIconType Search { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M10 10m-7 0a7 7 0 1 0 14 0a7 7 0 1 0 -14 0' />"
        + "<path d='M21 21l-6 -6' />");

    public static IIconType Settings { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M10.325 4.317c.426 -1.756 2.924 -1.756 3.35 0a1.724 1.724 0 0 0 2.573 1.066c1.543 -.94 3.31 .826 2.37 2.37a1.724 1.724 0 0 0 1.065 2.572c1.756 .426 1.756 2.924 0 3.35"
        + "a1.724 1.724 0 0 0 -1.066 2.573c.94 1.543 -.826 3.31 -2.37 2.37a1.724 1.724 0 0 0 -2.572 1.065c-.426 1.756 -2.924 1.756 -3.35 0a1.724 1.724 0 0 0 -2.573 -1.066c-1.543 .94 -3.31 -.826 -2.37 -2.37"
        + "a1.724 1.724 0 0 0 -1.065 -2.572c-1.756 -.426 -1.756 -2.924 0 -3.35a1.724 1.724 0 0 0 1.066 -2.573c-.94 -1.543 .826 -3.31 2.37 -2.37c1 .608 2.296 .07 2.572 -1.065z' />"
        + "<path d='M9 12a3 3 0 1 0 6 0a3 3 0 0 0 -6 0' />");

    public static IIconType Sun { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M12 12m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0' />"
        + "<path d='M3 12h1m8 -9v1m8 8h1m-9 8v1m-6.4 -15.4l.7 .7m12.1 -.7l-.7 .7m0 11.4l.7 .7m-12.1 -.7l-.7 .7' />");

    public static IIconType User { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0' />"
        + "<path d='M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2' />");
}

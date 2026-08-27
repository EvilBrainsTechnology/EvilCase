using TabBlazor;

namespace EvilBrains.EvilCase.App.Icons;

// TabBlazor ships no icon set. Path data comes from the Tabler icon set; add icons here as they are needed.
public static class AppIcons
{
    public static IIconType Clock { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M3 12a9 9 0 1 0 18 0a9 9 0 0 0 -18 0' />"
        + "<path d='M12 7v5l3 3' />");

    public static IIconType Download { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2 -2v-2' />"
        + "<path d='M7 11l5 5l5 -5' />"
        + "<path d='M12 4l0 12' />");

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

    public static IIconType Sun { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M12 12m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0' />"
        + "<path d='M3 12h1m8 -9v1m8 8h1m-9 8v1m-6.4 -15.4l.7 .7m12.1 -.7l-.7 .7m0 11.4l.7 .7m-12.1 -.7l-.7 .7' />");

    public static IIconType Trash { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M4 7l16 0' />"
        + "<path d='M10 11l0 6' />"
        + "<path d='M14 11l0 6' />"
        + "<path d='M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12' />"
        + "<path d='M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3' />");

    public static IIconType Upload { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M4 17v2a2 2 0 0 0 2 2h12a2 2 0 0 0 2 -2v-2' />"
        + "<path d='M7 9l5 -5l5 5' />"
        + "<path d='M12 4l0 12' />");

    public static IIconType User { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M8 7a4 4 0 1 0 8 0a4 4 0 0 0 -8 0' />"
        + "<path d='M6 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2' />");

    public static IIconType Users { get; } = new TablerIcon("<path stroke='none' d='M0 0h24v24H0z' fill='none' />"
        + "<path d='M9 7m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0' />"
        + "<path d='M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2' />"
        + "<path d='M16 3.13a4 4 0 0 1 0 7.75' />"
        + "<path d='M21 21v-2a4 4 0 0 0 -3 -3.85' />");
}

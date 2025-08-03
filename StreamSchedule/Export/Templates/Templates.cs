using StreamSchedule.Export.Data;

namespace StreamSchedule.Export.Templates;

public static class Templates
{
    public const string Divider = """
                                  <div class="verticalBar"></div>
                                  """;

    public const string EmotesBlock = """
                                      <div class="emoteGroup">
                                        <p class="emoteGroupName"> {0} </p>
                                        {1}
                                      </div>
                                      """;

    public const string EmoteUpdatesSummary = """
                                              <a class="header" href="https://twitch.tv/{0}/"> {0} </a> 's emotes updated
                                              """;
    
    public const string EmoteUpdatesStyleName = "EmoteUpdates";
    public const int EmoteUpdatesStyleVersion = 1;
    
    public  static readonly EmbeddedStyle EmoteUpdatesStyle = new()
    {
        Style = """
                    .emoteGroup { display: flex; flex-wrap: wrap; flex-direction: row; text-align: center; width: max-content; height: max-content; }
                    .emoteGroupName { font-size: large; font-weight: bold; width: 100%; }
                    .emoteTier { text-align: right; display: inline; position: absolute; background-color: var(--accentColor); font-size: large; margin: 0; border: var(--accentColor) solid 3px; border-radius: 3px; }
                    .emote { flex: 1 0 5%; margin: 1px; text-align: center; }
                    .emoteText { font-size: large; font-weight: bold; margin: 3px 3px; }
                    .emoteImage { max-width: 128px; height: auto; }
                    .verticalBar { background-color: var(--mainColor); width: 3px; }
                    a.header:link { color: var(--secondaryColor); text-decoration: none; }
                    a.header:visited { color: var(--secondaryColor); text-decoration: none; }
                    a.header:hover { color: var(--accentColor); text-decoration: underline; }
                    a.header:active { color: var(--secondaryColor); text-decoration: underline; }
                """,
        Name = EmoteUpdatesStyleName,
        Version = EmoteUpdatesStyleVersion
    };
}

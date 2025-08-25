using StreamSchedule.WebExport.Data;

namespace StreamSchedule.WebExport.Templates;

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
                                              <a class="header" href="https://twitch.tv/{0}"> {0} </a> 's emotes updated
                                              """;

    public const string EmoteUpdatesStyleName = "EmoteUpdates";
    public const int EmoteUpdatesStyleVersion = 2;

    public static readonly EmbeddedStyle EmoteUpdatesStyle = new()
    {
        Style = """
                
                    .rarity0 { background-color: var(--rarity-0); border: var(--rarity-0) solid 3px; color: black; }
                    .rarity1 { background-color: var(--rarity-1); border: var(--rarity-1) solid 3px; color: black; }
                    .rarity2 { background-color: var(--rarity-2); border: var(--rarity-2) solid 3px; color: black; }
                    .rarity3 { background-color: var(--rarity-3); border: var(--rarity-3) solid 3px; color: black; }
                    .rarity4 { background-color: var(--rarity-4); border: var(--rarity-4) solid 3px; color: black; }
                    .rarity5 { background-color: var(--rarity-5); border: var(--rarity-5) solid 3px; color: black; }
                    .emoteGroup { display: flex; flex-wrap: wrap; flex-direction: row; justify-content: center; text-align: center; width: max-content; height: max-content; }
                    .emoteGroupName { font-size: large; font-weight: bold; width: 100%; }
                    .emoteTier { text-align: right; display: inline; position: absolute; font-size: large; margin: 0; border-radius: 5px; }
                    .emote { margin: 1px; text-align: center; }
                    .emoteText { font-size: large; font-weight: bold; margin: 3px 3px; }
                    .emoteImage { max-width: 128px; height: auto; border-right: none !important; border-bottom: none !important; border-radius: 5px; background: none !important; }
                    .emote:hover .emoteTier { display: none; }
                    .emote:hover .emoteImage { border-color: rgba(0,0,0,0); }
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

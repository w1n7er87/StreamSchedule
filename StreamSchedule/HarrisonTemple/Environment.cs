using NeoSmart.Unicode;

namespace StreamSchedule.HarrisonTemple;

internal static class Environment
{
    internal static string[] Creatures { get; }
    internal static string[] Decorations { get; }

    static Environment()
    {
        Creatures =
        [
            Emoji.Alien.Sequence.AsString,
            Emoji.SpiderWeb.Sequence.AsString,
            Emoji.Ant.Sequence.AsString,
            Emoji.Cockroach.Sequence.AsString,
            Emoji.Fly.Sequence.AsString,
            Emoji.Worm.Sequence.AsString,
            Emoji.Cricket.Sequence.AsString,
            Emoji.Ghost.Sequence.AsString,
            Emoji.Lizard.Sequence.AsString,
            Emoji.Bug.Sequence.AsString,
            Emoji.LadyBeetle.Sequence.AsString,
            Emoji.Bat.Sequence.AsString,
            Emoji.Zombie.Sequence.AsString,
            Emoji.Rat.Sequence.AsString,
            Emoji.Butterfly.Sequence.AsString,
            Emoji.Beetle.Sequence.AsString,
            Emoji.Mouse.Sequence.AsString,
            "_",
        ];

        Decorations =
        [
            Emoji.Amphora.Sequence.AsString,
            Emoji.Cactus.Sequence.AsString,
            Emoji.Coffin.Sequence.AsString,
            Emoji.Ladder.Sequence.AsString,
            Emoji.Brick.Sequence.AsString,
            Emoji.Bone.Sequence.AsString,
            Emoji.Skull.Sequence.AsString,
            Emoji.FallenLeaf.Sequence.AsString,
            Emoji.ClassicalBuilding.Sequence.AsString,
            Emoji.Hole.Sequence.AsString,
            Emoji.Rock.Sequence.AsString,
            Emoji.FireExtinguisher.Sequence.AsString,
            Emoji.Construction.Sequence.AsString,
            Emoji.Sunflower.Sequence.AsString,
            Emoji.Rose.Sequence.AsString,
            Emoji.Tulip.Sequence.AsString,
            Emoji.Blossom.Sequence.AsString,
            "\ud83e\udeba",
            "\ud83e\udeb9",
            "_",
        ];
    }
}

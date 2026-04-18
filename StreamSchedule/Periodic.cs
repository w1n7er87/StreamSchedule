namespace StreamSchedule;

public class Periodic
{
    private static readonly List<Periodic> UpdateTargets = [];

    static Periodic()
    {
        Task.Run(Cycle);
    }

    protected Periodic()
    {
        UpdateTargets.Add(this);
    }

    private static async Task Cycle()
    {
        while (true)
        {
            await Task.Delay(50);
            foreach (Periodic updateTarget in UpdateTargets)
            {
                try { updateTarget.Update(); }
                catch (Exception e) { BotCore.Nlog.Error(e); }
            }
        }
    }

    protected virtual void Update() { }

    // i think this doesn't make much sense, since the very fact that the instance is in the list is holding it from being GC, so this would never be called, but i'll still leave this here ok 
    ~Periodic()
    {
        UpdateTargets.Remove(this);
    }
}

namespace RMS_EDF;

/// C — время исполнения, T — период, D — относительный дедлайн.

public class PeriodicTask
{
    public string Name { get; set; } = "";
    public double C { get; set; }
    public double T { get; set; }
    public double D { get; set; }

    public PeriodicTask() { }

    public PeriodicTask(string name, double c, double t, double d)
    {
        Name = name;
        C = c;
        T = t;
        D = d;
    }

    public override string ToString() =>
        $"{Name}(C={C}, T={T}, D={D})";
}

/// Отрезок выполнения на диаграмме Ганта. TaskName = null — простой.</summary>
public record GanttSegment(double Start, double End, string? TaskName)
{
    public bool IsIdle => TaskName is null;
}
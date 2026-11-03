using System;
using System.Collections.Generic;
using System.Linq;

namespace RMS_EDF;

public static class Scheduler
{
    private const double Eps = 1e-9;

    /// Коэффициент загрузки U = Σ(Ci / Ti).
    public static double Utilization(IEnumerable<PeriodicTask> tasks) =>
        tasks.Sum(t => t.C / t.T);

    /// Граница Liu &amp; Layland: n * (2^(1/n) − 1).
    public static double LiuLaylandBound(int n) =>
        n * (Math.Pow(2.0, 1.0 / n) - 1.0);

    /// Достаточный тест RMS по утилизации.
    public static bool RmsSchedulable(IReadOnlyList<PeriodicTask> tasks) =>
        Utilization(tasks) <= LiuLaylandBound(tasks.Count) + Eps;

    /// Условие планируемости EDF для неявных дедлайнов: U ≤ 1.
    public static bool EdfSchedulable(IEnumerable<PeriodicTask> tasks) =>
        Utilization(tasks) <= 1.0 + Eps;

    /// Анализ времени отклика:
    ///   R_{k+1} = C_i + Σ_{j∈hp(i)} ceil(R_k / T_j) * C_j
    /// Итерация останавливается при стабилизации R или выходе за D.
    public static double ResponseTime(PeriodicTask task, IEnumerable<PeriodicTask> higherPriority)
    {
        var hp = higherPriority.ToList();
        double r = task.C;

        for (int iter = 0; iter < 10_000; iter++)
        {
            double next = task.C +
                          hp.Sum(h => Math.Ceiling(r / h.T - Eps) * h.C);

            if (Math.Abs(next - r) < Eps) return next;
            if (next > task.D + Eps) return next;   // не укладывается в дедлайн
            r = next;
        }
        return r;
    }

    // Гиперпериод
    private static long Gcd(long a, long b) => b == 0 ? Math.Abs(a) : Gcd(b, a % b);
    private static long Lcm(long a, long b) => a / Gcd(a, b) * b;

    /// НОК периодов (для целочисленных T).
    public static double Hyperperiod(IEnumerable<PeriodicTask> tasks)
    {
        long result = 1;
        foreach (var t in tasks)
            result = Lcm(result, (long)Math.Round(t.T));
        return result;
    }

    // ---------- Диаграмма Ганта ----------

    /// Симуляция RMS на интервале [0, interval]. Приоритет — меньший T.
    /// step — шаг дискретизации симуляции.
    public static List<GanttSegment> Gantt(
        IReadOnlyList<PeriodicTask> tasks,
        double interval,
        double step = 0.5)
    {
        // RMS: приоритет по возрастанию T
        var order = tasks.OrderBy(t => t.T).ThenBy(t => t.Name).ToList();

        var nextRelease = tasks.ToDictionary(t => t.Name, _ => 0.0);
        var remaining = tasks.ToDictionary(t => t.Name, _ => 0.0);

        var segments = new List<GanttSegment>();
        string? current = null;
        double segStart = 0.0;

        for (double now = 0; now < interval - Eps; now += step)
        {
            // 1. Выпуск новых заданий
            foreach (var t in tasks)
                while (nextRelease[t.Name] <= now + Eps)
                {
                    remaining[t.Name] += t.C;
                    nextRelease[t.Name] += t.T;
                }

            // 2. Выбор задачи с наивысшим приоритетом среди готовых
            string? pick = null;
            foreach (var t in order)
                if (remaining[t.Name] > Eps) { pick = t.Name; break; }

            // 3. Фиксация переключения контекста
            if (pick != current)
            {
                if (now - segStart > Eps)
                    segments.Add(new GanttSegment(segStart, now, current));
                segStart = now;
                current = pick;
            }

            // 4. Расход кванта CPU
            if (pick != null)
                remaining[pick] = Math.Max(0.0, remaining[pick] - step);
        }

        // 5. Закрытие последнего отрезка
        if (interval - segStart > Eps)
            segments.Add(new GanttSegment(segStart, interval, current));

        return segments;
    }
}
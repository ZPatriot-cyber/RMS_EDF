using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RMS_EDF;

public static class GanttRenderer
{
    public static void Render(
        IReadOnlyList<GanttSegment> segments,
        IReadOnlyList<PeriodicTask> tasks,
        double interval)
    {
        // ---- Список отрезков ----
        Console.WriteLine("Сегменты выполнения:");
        foreach (var s in segments)
        {
            var who = s.IsIdle ? "idle" : s.TaskName!;
            Console.WriteLine($"  [{s.Start,6:F2} → {s.End,6:F2})   {who}");
        }
        Console.WriteLine();

        // ---- Наглядная цветная диаграмма ----
        int width = (int)Math.Round(interval);
        var ordered = tasks.OrderBy(t => t.T).ToList();

        // Ширина одной клетки (такта) в символах
        int cellWidth = 4;

        // 1. Шапка со шкалой времени
        Console.Write("      "); // Отступ под имена задач
        for (int i = 0; i < width; i++)
        {
            string timeMark = i.ToString();
            // Центрируем число внутри клетки
            Console.Write($" {timeMark} ".PadRight(cellWidth));
        }
        Console.WriteLine();

        // 2. Верхняя граница таблицы
        Console.Write("      ");
        for (int i = 0; i < width; i++)
        {
            Console.Write("┌───");
        }
        Console.WriteLine("┐");

        // 3. Строки задач
        string[] colors = { "\u001b[41m", "\u001b[42m", "\u001b[44m", "\u001b[45m", "\u001b[46m" }; // Красный, Зеленый, Синий, Фиолетовый, Голубой
        string reset = "\u001b[0m";

        for (int r = 0; r < ordered.Count; r++)
        {
            var t = ordered[r];

            // Вывод имени задачи
            Console.Write($"{t.Name,-5} │");

            for (int i = 0; i < width; i++)
            {
                bool running = segments.Any(s => s.TaskName == t.Name && s.Start < i + 1 && s.End > i);

                if (running)
                {
                    // Если задача работает — рисуем цветной блок
                    string color = colors[r % colors.Length];
                    Console.Write(color + " █ " + reset + "│");
                }
                else
                {
                    // Если спит — пустое место
                    Console.Write("   │");
                }
            }
            Console.WriteLine();

            // Разделитель между задачами
            Console.Write("      ");
            for (int i = 0; i < width; i++)
            {
                Console.Write("├───");
            }
            Console.WriteLine("┤");
        }

        // 4. Строка "Простой" (idle)
        Console.Write("idle  │");
        for (int i = 0; i < width; i++)
        {
            bool isIdle = segments.Any(s => s.IsIdle && s.Start < i + 1 && s.End > i);
            if (isIdle)
            {
                Console.Write(" . │"); // Точка для простоя
            }
            else
            {
                Console.Write("   │");
            }
        }
        Console.WriteLine();

        // 5. Нижняя граница
        Console.Write("      ");
        for (int i = 0; i < width; i++)
        {
            Console.Write("└───");
        }
        Console.WriteLine("┘");
    }
}
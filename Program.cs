using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RMS_EDF;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("=== RMS_EDF — планирование периодических задач (RMS / EDF) ===");
Console.WriteLine();

string path = args.Length > 0 ? args[0] : "data/tasks.json";
if (!File.Exists(path))
{
    Console.Error.WriteLine($"Файл не найден: {path}");
    return 1;
}

List<PeriodicTask> tasks;
try
{
    tasks = TaskLoader.Load(path);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Ошибка чтения файла: {ex.Message}");
    return 1;
}

if (tasks.Count == 0)
{
    Console.Error.WriteLine("Список задач пуст.");
    return 1;
}

Console.WriteLine($"Источник данных: {path}");
Console.WriteLine($"Количество задач: {tasks.Count}");
Console.WriteLine();

// ---------- Анализ времени отклика ----------
// RMS: меньший T — выше приоритет
var rmsOrder = tasks.OrderBy(t => t.T).ToList();

var responseTimes = new Dictionary<string, double>();
var meetsDeadline = new Dictionary<string, bool>();

for (int i = 0; i < rmsOrder.Count; i++)
{
    var task = rmsOrder[i];
    var higher = rmsOrder.Take(i).ToList();
    var R = Scheduler.ResponseTime(task, higher);

    responseTimes[task.Name] = R;
    meetsDeadline[task.Name] = R <= task.D + 1e-9;
}

// ---------- Таблица ----------
Console.WriteLine("Таблица анализа задач:");
Console.WriteLine("+------------+-------+-------+-------+----------+------------+");
Console.WriteLine($"| {"Задача",-10} | {"C",5} | {"T",5} | {"D",5} | {"R",8} | {"Уложилась",-10} |");
Console.WriteLine("+------------+-------+-------+-------+----------+------------+");

foreach (var t in tasks)
{
    var R = responseTimes[t.Name];
    var ok = meetsDeadline[t.Name] ? "да" : "нет";
    Console.WriteLine($"| {t.Name,-10} | {t.C,5} | {t.T,5} | {t.D,5} | {R,8} | {ok,-10} |");
}
Console.WriteLine("+------------+-------+-------+-------+----------+------------+");

// ---------- Загрузка ----------
double U = Scheduler.Utilization(tasks);
Console.WriteLine();
Console.WriteLine($"Коэффициент загрузки: U = {U:F4}");

// ---------- RMS ----------
double bound = Scheduler.LiuLaylandBound(tasks.Count);
bool rmsOk = Scheduler.RmsSchedulable(tasks);
bool rmsRta = rmsOrder.All(t => meetsDeadline[t.Name]);

Console.WriteLine();
Console.WriteLine("--- RMS (Rate Monotonic Scheduling) ---");
Console.WriteLine($"Тест Liu & Layland:  n = {tasks.Count},  граница = {bound:F4}");
Console.WriteLine(
    $"U = {U:F4} {(rmsOk ? "<=" : ">")} {bound:F4} => " +
    (rmsOk
        ? "набор планируем (достаточное условие выполнено)"
        : "тест по утилизации не пройден; далее — анализ времени отклика"));

Console.WriteLine(
    "Анализ времени отклика: " +
    (rmsRta
        ? "все задачи укладываются в дедлайны (планируем)"
        : "есть задачи с превышением дедлайна (НЕ планируем)"));

// ---------- EDF ----------
Console.WriteLine();
Console.WriteLine("--- EDF (Earliest Deadline First) ---");
bool edfOk = Scheduler.EdfSchedulable(tasks);
Console.WriteLine(
    $"U = {U:F4} {(edfOk ? "<=" : ">")} 1.0000 => " +
    (edfOk ? "набор планируем" : "набор НЕ планируем"));

// ---------- Gantt ----------
double hyper = Scheduler.Hyperperiod(tasks);
Console.WriteLine();
Console.WriteLine($"--- Диаграмма выполнения (RMS) на интервале [0, {hyper:F0}] ---");
var segments = Scheduler.Gantt(tasks, hyper);
GanttRenderer.Render(segments, tasks, hyper);

return 0;
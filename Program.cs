using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CityLife
{
    public enum EventPriority { Low = 1, Medium = 2, High = 3, Critical = 4 }
    public enum EventType { Traffic, Emergency, PublicTransport, Utility, Weather, Entertainment }

    public class CityEvent : IComparable<CityEvent>
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public EventType Type { get; set; }
        public EventPriority Priority { get; set; }
        public DateTime ScheduledTime { get; set; }
        public DateTime StartedTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Location { get; set; } = "";
        public bool IsCompleted { get; set; }
        public DateTime? CompletedTime { get; set; }
        public string WorkerId { get; set; } = "";

        public int CompareTo(CityEvent? other)
        {
            if (other == null) return 1;
            int cmp = other.Priority.CompareTo(this.Priority);
            return cmp != 0 ? cmp : this.ScheduledTime.CompareTo(other.ScheduledTime);
        }

        public async Task ExecuteAsync(CancellationToken token, string workerId)
        {
            WorkerId = workerId;
            StartedTime = DateTime.Now;
            Console.WriteLine($"[⏱️ {StartedTime:HH:mm:ss}] ▶️ {Name} | {Priority} | {Location}");
            
            for (int p = 10; p <= 100; p += 10)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay((int)(Duration.TotalMilliseconds / 10), token);
                Console.WriteLine($"   📊 {Name}: {p}%");
            }
            IsCompleted = true;
            CompletedTime = DateTime.Now;
            Console.WriteLine($"[✅ {CompletedTime:HH:mm:ss}] ✔️ Завершено: {Name} (Worker: {workerId})\n");
        }

        public double ActualDurationMinutes => 
            ((CompletedTime ?? DateTime.Now) - (StartedTime != default ? StartedTime : ScheduledTime)).TotalMinutes;

        public override string ToString() => $"[{Priority}] {Name} ({Type}) @ {Location}";
    }

    public class FullStatisticsReport
    {
        public DateTime SimulationStart { get; set; }
        public DateTime SimulationEnd { get; set; }
        public List<CityEvent> AllEvents { get; set; } = new();
        
        public int TotalEvents => AllEvents.Count;
        public int CompletedEvents => AllEvents.Count(e => e.IsCompleted);
        public int FailedEvents => AllEvents.Count(e => !e.IsCompleted);
        
        public Dictionary<EventPriority, int> ByPriority => 
            AllEvents.GroupBy(e => e.Priority).ToDictionary(g => g.Key, g => g.Count());
        
        public Dictionary<EventType, int> ByType => 
            AllEvents.GroupBy(e => e.Type).ToDictionary(g => g.Key, g => g.Count());
        
        public Dictionary<string, int> ByWorker => 
            AllEvents.Where(e => !string.IsNullOrEmpty(e.WorkerId))
                    .GroupBy(e => e.WorkerId).ToDictionary(g => g.Key, g => g.Count());
        
        public double AverageDuration => 
            AllEvents.Where(e => e.IsCompleted).Any() 
                ? AllEvents.Where(e => e.IsCompleted).Average(e => e.ActualDurationMinutes) 
                : 0;
        
        public double MinDuration => 
            AllEvents.Where(e => e.IsCompleted).Any() 
                ? AllEvents.Where(e => e.IsCompleted).Min(e => e.ActualDurationMinutes) 
                : 0;
        
        public double MaxDuration => 
            AllEvents.Where(e => e.IsCompleted).Any() 
                ? AllEvents.Where(e => e.IsCompleted).Max(e => e.ActualDurationMinutes) 
                : 0;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n╔════════════════════════════════════════════╗");
            sb.AppendLine($"║  📊 ПІДСУМКОВА СТАТИСТИКА ЗА ВСІ ПОДІЇ  ║");
            sb.AppendLine($"╚════════════════════════════════════════════╝");
            sb.AppendLine($"\n🕐 Період симуляції: {SimulationStart:HH:mm:ss} — {SimulationEnd:HH:mm:ss}");
            sb.AppendLine($"   Тривалість: {(SimulationEnd - SimulationStart).TotalSeconds:F1} сек");
            
            sb.AppendLine($"\n📋 Загальна інформація:");
            sb.AppendLine($"   🔹 Усього подій: {TotalEvents}");
            sb.AppendLine($"   🔹 ✅ Завершено: {CompletedEvents}");
            sb.AppendLine($"   🔹 ❌ Не завершено: {FailedEvents}");
            sb.AppendLine($"   🔹 Сер. тривалість: {AverageDuration:F2} хв");
            sb.AppendLine($"   🔹 Мін/Макс тривалість: {MinDuration:F2} / {MaxDuration:F2} хв");
            
            if (ByPriority.Any())
            {
                sb.AppendLine($"\n🔹 За пріоритетом:");
                foreach (var kvp in ByPriority.OrderByDescending(x => x.Key))
                    sb.AppendLine($"      {kvp.Key,8}: {kvp.Value,3} подій");
            }
            
            if (ByType.Any())
            {
                sb.AppendLine($"\n🔹 За типом події:");
                foreach (var kvp in ByType.OrderBy(x => x.Key))
                    sb.AppendLine($"      {kvp.Key,15}: {kvp.Value,3} подій");
            }
            
            if (ByWorker.Any())
            {
                sb.AppendLine($"\n🔹 Розподіл по воркерах:");
                foreach (var kvp in ByWorker.OrderBy(x => x.Key))
                    sb.AppendLine($"      Worker {kvp.Key,2}: {kvp.Value,3} подій");
            }
            
            var completed = AllEvents.Where(e => e.IsCompleted).OrderBy(e => e.CompletedTime).ToList();
            if (completed.Any())
            {
                sb.AppendLine($"\n📋 Детальний список завершених подій (макс. 15):");
                sb.AppendLine($"   {"№",-3} {"Час",-10} {"Пріоритет",-10} {"Тип",-12} {"Назва",-18} {"Локація",-12} {"Трив.сек",-8}");
                sb.AppendLine($"   {"",-3} {"",-10} {"",-10} {"",-12} {"",-18} {"",-12} {"",-8}");
                
                int num = 1;
                foreach (var evt in completed.Take(15))
                {
                    sb.AppendLine($"   {num++, -3} {evt.CompletedTime?.ToString("HH:mm:ss"),-10} " +
                                $"{evt.Priority,-10} {evt.Type,-12} {evt.Name,-18} {evt.Location,-12} " +
                                $"{evt.ActualDurationMinutes * 60, -8:F1}");
                }
                if (completed.Count > 15)
                    sb.AppendLine($"   ... ще {completed.Count - 15} подій");
            }
            
            return sb.ToString();
        }
    }

    public class EventStatistics
    {
        private readonly List<CityEvent> _allEvents = new();
        private readonly object _lock = new();
        public DateTime SimulationStart { get; set; } = DateTime.Now;
        public DateTime SimulationEnd { get; set; } = DateTime.Now;

        public void AddEvent(CityEvent e)
        {
            lock (_lock) { if (!_allEvents.Any(x => x.Id == e.Id)) _allEvents.Add(e); }
        }

        public void RecordCompletion(CityEvent e)
        {
            lock (_lock)
            {
                var existing = _allEvents.FirstOrDefault(x => x.Id == e.Id);
                if (existing != null)
                {
                    existing.IsCompleted = e.IsCompleted;
                    existing.CompletedTime = e.CompletedTime;
                    existing.WorkerId = e.WorkerId;
                    existing.StartedTime = e.StartedTime;
                }
            }
        }

        public FullStatisticsReport GetFullReport()
        {
            lock (_lock)
            {
                return new FullStatisticsReport
                {
                    SimulationStart = SimulationStart,
                    SimulationEnd = SimulationEnd,
                    AllEvents = new List<CityEvent>(_allEvents)
                };
            }
        }
    }

    public class EventQueue
    {
        private readonly PriorityQueue<CityEvent, int> _pq = new();
        private readonly object _lock = new();

        public void Enqueue(CityEvent e)
        {
            lock (_lock)
            {
                int key = 5 - (int)e.Priority;
                _pq.Enqueue(e, key);
                Console.WriteLine($"[📥] Черга+: {e}");
            }
        }

        public async Task<CityEvent?> DequeueAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                lock (_lock)
                {
                    if (_pq.TryDequeue(out var e, out _))
                    {
                        Console.WriteLine($"[📤] Черга-: {e}");
                        return e;
                    }
                }
                await Task.Delay(50, token);
            }
            return null;
        }

        public int Count { get { lock (_lock) return _pq.Count; } }
        public bool IsEmpty { get { lock (_lock) return _pq.Count == 0; } }
    }

    public class EventProcessor
    {
        private readonly EventQueue _queue;
        private readonly EventStatistics _stats;
        private readonly SemaphoreSlim _semaphore;
        private readonly Random _rnd = new();
        private readonly bool _isAsync;

        public EventProcessor(EventQueue q, EventStatistics s, int maxParallel = 3, bool isAsync = true)
        {
            _queue = q; _stats = s; _isAsync = isAsync;
            _semaphore = new SemaphoreSlim(maxParallel);
        }

        public async Task StartAsync(int minutes, CancellationToken token)
        {
            var endTime = DateTime.Now.AddMinutes(minutes);
            _stats.SimulationStart = DateTime.Now;
            Console.WriteLine($"\n🚀 Старт: {minutes} хв | {_semaphore.CurrentCount} воркерів | Завершення о {endTime:HH:mm:ss}");
            
            var tasks = new List<Task>();
            
            // Воркери
            for (int i = 0; i < _semaphore.CurrentCount; i++)
                tasks.Add(WorkerAsync(i + 1, endTime, token));
            
            // Генератор
            tasks.Add(GenerateEventsAsync(endTime, token));
            
            // Статистика
            tasks.Add(ReportStatsAsync(endTime, token));
            
            // Чекаємо завершення часу симуляції
            await Task.Delay(TimeSpan.FromMinutes(minutes));
            Console.WriteLine("\n⏰ Час генерації вийшов! Завершення поточних подій...");
            
            // Чекаємо доки черга спорожніє або пройде 10 сек
            var drainEnd = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < drainEnd && !_queue.IsEmpty)
                await Task.Delay(200);
            
            _stats.SimulationEnd = DateTime.Now;
            await Task.WhenAll(tasks);
            Console.WriteLine("✅ Симуляція завершена.");
        }

        private async Task WorkerAsync(int id, DateTime endTime, CancellationToken token)
        {
            while (DateTime.Now < endTime || !_queue.IsEmpty)
            {
                if (token.IsCancellationRequested) break;
                await _semaphore.WaitAsync(token);
                try
                {
                    var evt = await _queue.DequeueAsync(token);
                    if (evt != null)
                    {
                        Console.WriteLine($"[👷#{id}] Обробка: {evt.Name}");
                        await evt.ExecuteAsync(token, $"W{id}");
                        _stats.RecordCompletion(evt);
                    }
                }
                catch (OperationCanceledException) { break; }
                finally { _semaphore.Release(); }
            }
        }

        private async Task GenerateEventsAsync(DateTime endTime, CancellationToken token)
        {
            while (DateTime.Now < endTime && !token.IsCancellationRequested)
            {
                await Task.Delay(_rnd.Next(400, 1500), token);
                if (DateTime.Now >= endTime) break;
                
                var evt = CreateRandomEvent();
                _queue.Enqueue(evt);
                _stats.AddEvent(evt);
            }
            Console.WriteLine("⏹️ Генерацію подій зупинено");
        }

        private async Task ReportStatsAsync(DateTime endTime, CancellationToken token)
        {
            while (DateTime.Now < endTime && !token.IsCancellationRequested)
            {
                await Task.Delay(10000, token);
                var rep = _stats.GetFullReport();
                if (rep.TotalEvents > 0)
                {
                    var remaining = (endTime - DateTime.Now).TotalSeconds;
                    Console.WriteLine($"\n📊 Проміжна статистика: {rep.CompletedEvents}/{rep.TotalEvents} завершено (залишилось {remaining:F0} сек)");
                }
            }
        }

        private CityEvent CreateRandomEvent()
        {
            var names = new[] { "Ремонт дороги", "Аварія", "Фестиваль", "Зміна світлофора", 
                               "Підвезення води", "Концерт", "Шторм", "Техобслуговування" };
            var locs = new[] { "Центр", "Вокзал", "Парк", "Ліс", "Промзона", "ЖК", "Набережна", "Університет" };
            
            return new CityEvent
            {
                Name = names[_rnd.Next(names.Length)],
                Type = (EventType)_rnd.Next(Enum.GetValues<EventType>().Length),
                Priority = (EventPriority)_rnd.Next(1, 5),
                ScheduledTime = DateTime.Now,
                Duration = TimeSpan.FromSeconds(_rnd.Next(2, 6)),
                Location = locs[_rnd.Next(locs.Length)]
            };
        }
    }

    public class Lab10T2
    {
        private EventQueue? _queue;
        private EventStatistics? _stats;
        private EventProcessor? _processor;
        private CancellationTokenSource? _cts;
        private bool _isAsyncMode = true;

        public async Task RunAsync(int simulationMinutes = 2, int maxWorkers = 3)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════╗");
            Console.WriteLine("║   🏙️  ЖИТТЯ МІСТА — Симулятор    ║");
            Console.WriteLine("╚════════════════════════════════════╝");
            Console.ResetColor();

            try
            {
                _queue = new EventQueue();
                _stats = new EventStatistics();
                _processor = new EventProcessor(_queue, _stats, maxWorkers, _isAsyncMode);
                _cts = new CancellationTokenSource();

                Console.CancelKeyPress += (s, e) => { e.Cancel = true; Stop(); };

                await _processor!.StartAsync(simulationMinutes, _cts!.Token);
                PrintFinalReport();
            }
            catch (OperationCanceledException) { Console.WriteLine("\n⏹️ Зупинено користувачем"); }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Помилка: {ex.Message}");
                Console.ResetColor();
            }
            finally { Cleanup(); }
        }

        public void Run() => RunAsync().GetAwaiter().GetResult();

        private void PrintFinalReport()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (_stats != null)
            {
                var rep = _stats.GetFullReport();
                Console.WriteLine(rep.ToString());
            }
            Console.ResetColor();
        }

        public void Stop()
        {
            _cts?.Cancel();
            if (_stats != null) _stats.SimulationEnd = DateTime.Now;
            Console.WriteLine("\n🔄 Завершення...");
        }

        private void Cleanup()
        {
            _cts?.Dispose();
            Console.WriteLine("✅ Ресурси звільнено");
        }

        public void SetAsyncMode(bool isAsync) => _isAsyncMode = isAsync;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            ShowMenu();
        }

        static void ShowMenu()
        {
            while (true)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("╔══════════════════════════════════════════╗");
                Console.WriteLine("║     🏙️  МЕНЮ: ЖИТТЯ МІСТА  ║");
                Console.WriteLine("╚══════════════════════════════════════════╝");
                Console.ResetColor();
                
                Console.WriteLine("\n📋 Оберіть режим симуляції:");
                Console.WriteLine("   1️⃣  Асинхронний режим (рекомендовано)");
                Console.WriteLine("   2️⃣  Синхронний режим");
                Console.WriteLine("   3️⃣  Налаштувати параметри");
                Console.WriteLine("   0️⃣  Вихід");
                
                Console.Write("\n   Ваш вибір → ");
                var key = Console.ReadKey();
                Console.WriteLine();

                switch (key.KeyChar)
                {
                    case '1': RunSimulation(isAsync: true); break;
                    case '2': RunSimulation(isAsync: false); break;
                    case '3': ShowSettings(); continue;
                    case '0':
                        Console.WriteLine("\n👋 Дякуємо за використання!");
                        return;
                    default:
                        Console.WriteLine("   ❌ Невірний вибір...");
                        Thread.Sleep(1000);
                        continue;
                }
                
                Console.WriteLine("\n🔁 [Enter] — Новий запуск  |  [Esc] — Вихід");
                var next = Console.ReadKey();
                if (next.Key == ConsoleKey.Escape) break;
            }
        }

        static void ShowSettings()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚙️  НАЛАШТУВАННЯ СИМУЛЯЦІЇ");
            Console.ResetColor();
            
            Console.Write("\n   Тривалість (1-10 хв) [2]: ");
            var minInput = Console.ReadLine();
            int minutes = int.TryParse(minInput, out var m) && m >= 1 && m <= 10 ? m : 2;
            
            Console.Write("   Воркери (1-5) [3]: ");
            var wInput = Console.ReadLine();
            int workers = int.TryParse(wInput, out var w) && w >= 1 && w <= 5 ? w : 3;
            
            Console.Write("   Режим: [1] Async / [2] Sync: ");
            var modeInput = Console.ReadKey().KeyChar;
            bool isAsync = modeInput != '2';
            
            Console.WriteLine($"\n   ✅ Збережено: {minutes} хв, {workers} воркерів, {(isAsync ? "Async" : "Sync")}");
            Console.WriteLine("   Натисніть будь-яку клавішу для запуску...");
            Console.ReadKey();
            
            RunSimulation(isAsync, minutes, workers);
        }

        static void RunSimulation(bool isAsync, int minutes = 2, int workers = 3)
        {
            var lab = new Lab10T2();
            lab.SetAsyncMode(isAsync);
            Console.Clear();
            lab.RunAsync(simulationMinutes: minutes, maxWorkers: workers).Wait();
            Console.WriteLine("\n💾 Натисніть клавішу для повернення в меню...");
            Console.ReadKey();
        }
    }
}
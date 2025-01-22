using Akka.Actor;
using DistributedPrimeCalculator.Common.Messages;
using System.Collections.Generic;
using System;
using System.Linq;
using Akka.Event;

public class MasterActor : ReceiveActor, IWithTimers
{
    public ITimerScheduler Timers { get; set; } = null!;

    private const int MaxWorkerLoad = 5; // Maximální počet úloh na workera
    private Queue<WorkMessage> _pendingWork = new Queue<WorkMessage>();
    private Dictionary<int, WorkMessage> _activeWork = new Dictionary<int, WorkMessage>();
    private Dictionary<string, int> _workerLoad = new Dictionary<string, int>();
    private Dictionary<int, string> _jobToWorker = new Dictionary<int, string>(); // Mapování job ID na worker

    public class WorkerHealthCheck
    {
        public string WorkerAddress { get; set; }
        public WorkerHealthCheck(string workerAddress)
        {
            WorkerAddress = workerAddress;
        }
    }
    
    public MasterActor(List<string> workerAddresses)
    {
        foreach(var worker in workerAddresses)
        {
            _workerLoad[worker] = 0;
        }

        Receive<StartJobMessage>(message =>
        {
            for (int i = message.Start; i < message.End; i += message.BatchSize)
            {
                var end = Math.Min(i + message.BatchSize, message.End);
                var workMessage = new WorkMessage(GenerateWorkId(), i, end);
                _pendingWork.Enqueue(workMessage);
            }
            
            DistributeWork();
        });

        Receive<ResultMessage>(message =>
        {
            if (_activeWork.Remove(message.JobId))
            {
                var worker = GetWorkerForJob(message.JobId);
                if (worker != null)
                {
                    _workerLoad[worker]--;
                    _jobToWorker.Remove(message.JobId);
                }
                
                Console.WriteLine($"Přijat výsledek pro práci {message.JobId} od workera {worker}. Počet nalezených prvočísel: {message.Primes.Count}");
                
                DistributeWork();
            }
        });

        Receive<WorkerHealthCheck>(message =>
        {
            foreach (var worker in _workerLoad.Keys.ToList())
            {
                var selection = Context.ActorSelection(worker);
                selection.Tell(new WorkerHealthCheck(worker), Self);
            }
        });

        Receive<WorkerUnavailableMessage>(message =>
        {
            if (_workerLoad.ContainsKey(message.WorkerAddress))
            {
                Console.WriteLine($"Worker {message.WorkerAddress} je nedostupný.");
                _workerLoad.Remove(message.WorkerAddress);

                var jobsToReassign = _jobToWorker
                    .Where(j => j.Value == message.WorkerAddress)
                    .Select(j => j.Key)
                    .ToList();

                foreach (var jobId in jobsToReassign)
                {
                    if (_activeWork.TryGetValue(jobId, out var work))
                    {
                        _pendingWork.Enqueue(work);
                        _activeWork.Remove(jobId);
                        _jobToWorker.Remove(jobId);
                    }
                }

                DistributeWork();
            }
        });

        Receive<CheckTimeoutMessage>(message =>
        {
            var timedOutJobs = _activeWork
                .Where(w => (DateTime.UtcNow - w.Value.Timestamp).TotalSeconds > 10)
                .Select(w => w.Key)
                .ToList();

            foreach (var jobId in timedOutJobs)
            {
                if (_activeWork.TryGetValue(jobId, out var work))
                {
                    var worker = _jobToWorker[jobId];
                    Console.WriteLine($"Práce {jobId} od workera {worker} vypršela.");
                    _pendingWork.Enqueue(work);
                    _activeWork.Remove(jobId);
                    _jobToWorker.Remove(jobId);
                    _workerLoad[worker]--;
                }
            }

            DistributeWork();
        });

        Receive<DeadLetter>(message =>
        {
            if (message.Message is WorkerHealthCheck healthCheck)
            {
                Console.WriteLine($"Worker {healthCheck.WorkerAddress} je nedostupný.");
                Self.Tell(new WorkerUnavailableMessage(healthCheck.WorkerAddress));
            }
        });
    }

    private string? GetWorkerForJob(int jobId)
    {
        return _jobToWorker.TryGetValue(jobId, out var worker) ? worker : null;
    }

    private void DistributeWork()
    {
        var random = new Random();
        var shuffledWorkers = _workerLoad.Keys.OrderBy(x => random.Next()).ToList();

        while (_pendingWork.Count > 0)
        {
            var availableWorker = GetLeastLoadedWorker(shuffledWorkers);
            if (availableWorker == null || _workerLoad[availableWorker] >= MaxWorkerLoad)
                break;

            var work = _pendingWork.Dequeue();
            AssignWorkToWorker(work, availableWorker);
            _workerLoad[availableWorker]++;
            _jobToWorker[work.JobId] = availableWorker;
        }
    }

    private string? GetLeastLoadedWorker(List<string> shuffledWorkers)
    {
        return shuffledWorkers
            .OrderBy(w => _workerLoad[w])
            .FirstOrDefault();
    }

    private void AssignWorkToWorker(WorkMessage work, string worker)
    {
        work.Timestamp = DateTime.UtcNow;
        _activeWork[work.JobId] = work;

        var selection = Context.ActorSelection(worker);
        selection.Tell(work, Self);
        Console.WriteLine($"Práce {work.JobId} přiřazena workeru na {worker}");
        Timers.StartSingleTimer($"timeout-{work.JobId}", new CheckTimeoutMessage(), TimeSpan.FromSeconds(10));
    }

    private int GenerateWorkId()
    {
        return Guid.NewGuid().GetHashCode();
    }
}

public class CheckTimeoutMessage { }
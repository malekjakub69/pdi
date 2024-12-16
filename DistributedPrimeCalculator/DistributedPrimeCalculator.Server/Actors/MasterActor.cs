using Akka.Actor;
using DistributedPrimeCalculator.Common.Messages;
using System.Collections.Generic;
using System;
using System.Linq;

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

        Receive<WorkerHealthCheck>(_ => {
            // Implementovat health check
        });
    }

    private string? GetWorkerForJob(int jobId)
    {
        return _jobToWorker.TryGetValue(jobId, out var worker) ? worker : null;
    }

    private void DistributeWork()
    {
        while(_pendingWork.Count > 0)
        {
            var availableWorker = GetLeastLoadedWorker();
            if (availableWorker == null || _workerLoad[availableWorker] >= MaxWorkerLoad)
                break;

            var work = _pendingWork.Dequeue();
            AssignWorkToWorker(work, availableWorker);
            _workerLoad[availableWorker]++;
            _jobToWorker[work.JobId] = availableWorker;
        }
    }

    private string? GetLeastLoadedWorker()
    {
        return _workerLoad
            .OrderBy(w => w.Value)
            .FirstOrDefault().Key;
    }

    private void AssignWorkToWorker(WorkMessage work, string worker)
    {
        work.Timestamp = DateTime.UtcNow;
        _activeWork[work.JobId] = work;

        var selection = Context.ActorSelection(worker);
        selection.Tell(work, Self);
        Console.WriteLine($"Práce {work.JobId} přiřazena workeru na {worker}");
    }

    private int GenerateWorkId()
    {
        return Guid.NewGuid().GetHashCode();
    }
}

public class CheckTimeoutMessage { }
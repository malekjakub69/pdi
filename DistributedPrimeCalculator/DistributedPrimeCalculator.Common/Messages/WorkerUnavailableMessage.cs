namespace DistributedPrimeCalculator.Common.Messages
{
    public class WorkerUnavailableMessage
    {
        public string WorkerAddress { get; }

        public WorkerUnavailableMessage(string workerAddress)
        {
            WorkerAddress = workerAddress;
        }
    }
} 
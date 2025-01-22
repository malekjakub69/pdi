 using Akka.Actor;
using DistributedPrimeCalculator.Common.Messages;

public class WorkerActor : ReceiveActor
{
    public WorkerActor()
    {
        Receive<WorkMessage>(message =>
        {
            Console.WriteLine($"Worker přijal práci: JobId={message.JobId}, rozsah {message.Start}-{message.End}");
            
            var primes = FindPrimesInRange(message.Start, message.End);
            
            Console.WriteLine($"Worker dokončil práci {message.JobId}, nalezeno {primes.Count} prvočísel");
            
            var result = new ResultMessage(message.JobId, primes, message.Start, message.End);
            Sender.Tell(result);
            
            Console.WriteLine($"Worker odeslal výsledky pro JobId={message.JobId}");
        });
    }

    private List<int> FindPrimesInRange(int start, int end)
    {
        return Enumerable.Range(start, end - start)
                         .Where(IsPrime)
                         .ToList();
    }

    private bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;
        
        var boundary = (int)Math.Sqrt(number);
        for (int i = 3; i <= boundary; i += 2)
        {
            if (number % i == 0) return false;
        }
        return true;
    }
}
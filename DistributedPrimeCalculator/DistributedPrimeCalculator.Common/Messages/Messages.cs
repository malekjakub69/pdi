// V Messages.cs
using System;
using System.Collections.Generic;

namespace DistributedPrimeCalculator.Common.Messages;

[Serializable]
public record StartJobMessage(int Start, int End, int BatchSize);

[Serializable]
public record WorkMessage(int JobId, int Start, int End)
{
    public DateTime Timestamp { get; set; }
}

[Serializable]
public record ResultMessage(int JobId, List<int> Primes, int Start, int End);
using Microsoft.AspNetCore.Mvc;
using Akka.Actor;
using DistributedPrimeCalculator.Common.Messages;

[ApiController]
[Route("api/[controller]")]
public class CalculationController : ControllerBase
{
    private readonly IActorRef _masterActor;

    public CalculationController(IActorRef masterActor)
    {
        _masterActor = masterActor;
    }

    [HttpPost("start")]
    public IActionResult StartCalculation([FromQuery] int start, [FromQuery] int end, [FromQuery] int batchSize)
    {
        Console.WriteLine($"Přijat požadavek na výpočet: start={start}, end={end}, batchSize={batchSize}");
        var message = new StartJobMessage(start, end, batchSize);
        _masterActor.Tell(message);
        Console.WriteLine("Zpráva odeslána master actorovi");
        return Ok("Výpočet zahájen");
    }
}
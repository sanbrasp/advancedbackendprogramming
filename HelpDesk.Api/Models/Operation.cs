using HelpDesk.Api.Interfaces;

namespace HelpDesk.Api.Models;

internal class Operation : IOperation, IOperationTransient, IOperationScoped, IOperationSingleton
{
    // private static readonly int _nextId = 1;

    public int OperationId { get; init; }

    public Operation()
    {
        // OperationId = _nextId++;
        OperationId = Random.Shared.Next(1000, 10000);
    }
}
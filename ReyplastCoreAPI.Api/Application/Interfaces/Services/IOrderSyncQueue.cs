namespace ReyplastCoreAPI.Application.Interfaces.Services;

public interface IOrderSyncQueue
{
    bool TryEnqueueOsis(Guid executionId);
    bool TryCancel(Guid executionId);
}

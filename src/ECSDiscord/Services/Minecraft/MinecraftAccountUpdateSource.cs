using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECSDiscord.Services.Minecraft;

/**
 * Concurrency helper to allow multiple tasks to wait for updates to Minecraft accounts.
 */
public class MinecraftAccountUpdateSource
{
    private readonly List<TaskCompletionSource<bool>> _waitingTasks;

    public MinecraftAccountUpdateSource()
    {
        _waitingTasks = new List<TaskCompletionSource<bool>>();
    }
    
    public void SignalUpdate()
    {
        lock (_waitingTasks)
        {
            foreach (var taskCompletionSource in _waitingTasks)
            {
                taskCompletionSource.TrySetResult(true);
            }

            _waitingTasks.Clear();
        }
    }
    
    public async Task WaitForUpdateAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        lock (_waitingTasks)
        {
            _waitingTasks.Add(tcs);
        }
        
        using (ct.Register(() => tcs.TrySetCanceled()))
        {
            await tcs.Task;
            
            if (tcs.Task.IsCanceled)
            {
                throw new OperationCanceledException("Operation was canceled.");
            }
        }
    }
}
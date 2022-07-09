using System;
using System.Threading.Tasks;
using UnityEngine;

public sealed class WaitForTask : CustomYieldInstruction
{
    public WaitForTask(Task task)
    {
        this.task = task;
    }

    private Task task;
    public override bool keepWaiting => !task.IsCompleted;
}

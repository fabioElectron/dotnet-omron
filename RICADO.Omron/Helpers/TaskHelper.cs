using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RICADO.Omron.Helpers
{
    internal static class TaskHelper
    {

        static ILogger _logger;

        static TaskHelper()
        {
            _logger = HostServices.CreateLogger();
        }

        private static void CancelCts(CancellationTokenSource cts, string taskName)
        {
            try
            {
                cts.Cancel();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot signal cancel token for {name}", taskName);
            }
        }

        private static void DisposeTask(ref Task task, string taskName, int timeout)
        {
            try
            {
                if (task != null)
                {
                    if (!task.IsCanceled && !task.IsCompleted && !task.IsCompletedSuccessfully && !task.IsFaulted)
                    {
                        bool ok = task.Wait(timeout);
                        if (!ok)
                            _logger?.LogError("{name}", taskName);
                    }
                    task.Dispose();
                    task = null;
                    //_logger?.LogDebug($"task {taskNames[i]} disposed");
                }
                //else
                //    _logger?.LogDebug($"task {taskNames[i]} already disposed");
            }
            catch (AggregateException ae)
            {
                ae.Handle(ce => ce is OperationCanceledException || ce is TaskCanceledException);
                //if (ae.InnerException is OperationCanceledException || ae.InnerExceptions.Any(x => x is OperationCanceledException))
                //    return;
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException || ex is TaskCanceledException))
                    _logger?.LogError(ex, "**** Error disposing task {name}", taskName);
            }
        }

        private static void DisposeCts(ref CancellationTokenSource cts)
        {
            GC.Collect(3, GCCollectionMode.Forced, true);
            cts.Dispose();
            GC.Collect(3, GCCollectionMode.Forced, true);
            cts = new CancellationTokenSource();
        }



        public static void DisposeTask(ref Task task, ref CancellationTokenSource cts, string taskName, int timeout = 5000)
        {
            if (task != null)
            {
                CancelCts(cts, taskName);
                try
                {
                    DisposeTask(ref task, taskName, timeout);
                    DisposeCts(ref cts);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "DisposeTaskList Cannot dispose {name}", taskName);
                }
            }
            //DisposeTaskList(new List<Task>() { task }, ref cts, new List<string>() { taskName }, timeout);
        }

        public static void DisposeTaskList(List<Task> taskList, ref CancellationTokenSource cts, List<string> taskNames, int timeout = 5000)
        {
            CancelCts(cts, taskNames[0]);
            try
            {
                for (int i = taskList.Count - 1; i >= 0; i--)
                {
                    var task = taskList[i];
                    DisposeTask(ref task, taskNames[i], timeout);
                }
                DisposeCts(ref cts);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "DisposeTaskList Cannot dispose {name}", taskNames[0]);
            }
        }

        /// <summary>
        /// Helper method to start a long running task
        /// </summary>
        /// <param name="action"> Task body </param>
        /// <param name="cts"> Cancellation token source </param>
        /// <returns> The started task </returns>
        public static Task StartLongRunningTask(Action action, CancellationTokenSource cts)
        {
            var task = new Task(action, cts.Token, TaskCreationOptions.LongRunning);
            task.Start();
            return task;
        }

        /// <summary>
        /// Helper method to correctly implement a long running task body
        /// </summary>
        /// <param name="bodyAction"> Loop inner code. Return true if the while loop must be graceffully exited </param>
        /// <param name="returnAction"> Code to be executes upon graceful exit </param>
        /// <param name="cts"> Cancellation token source </param>
        /// <param name="dueTimeMilliseconds"> Time to be awaited after each loop </param>
        /// <exception cref="OperationCanceledException"> Exception thrown upon cancellatino token set, as per Microsoft guidelines </exception>
        public static void TaskImplementation(Func<bool> bodyAction, Action returnAction, CancellationTokenSource cts, int dueTimeMilliseconds)
        {
            while (true)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    returnAction?.Invoke();
                    cts.Token.ThrowIfCancellationRequested();
                }

                if (bodyAction.Invoke())
                {
                    returnAction?.Invoke();
                    return;
                }

                if (cts.IsCancellationRequested)
                {
                    returnAction?.Invoke();
                    throw new OperationCanceledException();
                }

                if (cts.Token.WaitHandle.WaitOne(dueTimeMilliseconds))
                {
                    returnAction?.Invoke();
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }

        /// <summary>
        /// Helper method to correctly implement a long running task body calling async methods
        /// </summary>
        /// <param name="bodyAction"> Loop inner code. Return true if the while loop must be graceffully exited </param>
        /// <param name="returnAction"> Code to be executes upon graceful exit </param>
        /// <param name="cts"> Cancellation token source </param>
        /// <param name="dueTimeMilliseconds"> Time to be awaited after each loop </param>
        /// <exception cref="OperationCanceledException"> Exception thrown upon cancellatino token set, as per Microsoft guidelines </exception>
        public static void AsyncTaskImplementation(Func<Task<bool>> bodyAction, Action returnAction, CancellationTokenSource cts, int dueTimeMilliseconds)
        {
            while (true)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    returnAction?.Invoke();
                    cts.Token.ThrowIfCancellationRequested();
                }

                if (bodyAction().Result)
                {
                    returnAction?.Invoke();
                    return;
                }

                if (cts.IsCancellationRequested)
                {
                    returnAction?.Invoke();
                    throw new OperationCanceledException();
                }

                if (cts.Token.WaitHandle.WaitOne(dueTimeMilliseconds))
                {
                    returnAction?.Invoke();
                    cts.Token.ThrowIfCancellationRequested();
                }
            }
        }

    }
}

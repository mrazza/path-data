namespace PathApi.Server.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PathApi.Server;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit tests for the <see cref="StartupTaskExecutor"/> class.
    /// </summary>
    [TestClass]
    public sealed class StartupTaskExecutorTest
    {
        private sealed class SimpleStartupTask : IStartupTask
        {
            public readonly Func<Task> callback;

            public SimpleStartupTask(Func<Task> callback)
            {
                this.callback = callback;
            }

            public async Task OnStartup()
            {
                await this.callback();
            }
        }

        [TestMethod]
        public async Task ExecutesEachTask()
        {
            int callCount1 = 0;
            int callCount2 = 0;
            var startupTaskExecutor = new StartupTaskExecutor(new[]
            {
                new SimpleStartupTask(() => { callCount1++; return Task.CompletedTask; }),
                new SimpleStartupTask(() => { callCount2++; return Task.CompletedTask; })
            });

            await startupTaskExecutor.ExecuteTasks();
            Assert.AreEqual(1, callCount1);
            Assert.AreEqual(1, callCount2);
        }

        [TestMethod]
        public async Task AsyncExecution()
        {
            int callCount1 = 0;
            int callCount2 = 0;
            int callCount3 = 0;
            TaskCompletionSource<object> checkpointLatch = new TaskCompletionSource<object>();
            TaskCompletionSource<object> blockingLatch = new TaskCompletionSource<object>();

            var startupTaskExecutor = new StartupTaskExecutor(new[]
            {
                new SimpleStartupTask(() => { callCount1++; checkpointLatch.SetResult(null); return Task.CompletedTask; }),
                new SimpleStartupTask(async () => { await blockingLatch.Task; callCount2++; }),
                new SimpleStartupTask(() => { callCount3++; return Task.CompletedTask; })
            });

            var executionTask = startupTaskExecutor.ExecuteTasks();
            await checkpointLatch.Task;
            Assert.AreEqual(1, callCount1);
            Assert.AreEqual(0, callCount2);
            Assert.AreEqual(1, callCount3);
            Assert.IsFalse(executionTask.IsCompleted);

            blockingLatch.SetResult(null);
            await executionTask;
            Assert.AreEqual(1, callCount1);
            Assert.AreEqual(1, callCount2);
            Assert.AreEqual(1, callCount3);
            Assert.IsTrue(executionTask.IsCompleted);
        }
    }
}
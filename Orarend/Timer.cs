using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleTimerPortable
{
    internal delegate void TimerCallback(object state);

    internal sealed class Timer : CancellationTokenSource, IDisposable
    {
        public Timer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            this.callback = callback;
            this.state = state;
            start(dueTime, period);
        }

        private TimerCallback callback;
        private object state;
        private void start(TimeSpan dueTime, TimeSpan period)
        {
            Task.Delay(dueTime, Token).ContinueWith(async (t, s) =>
            {
                var tuple = (Tuple<TimerCallback, object>)s;

                while (true)
                {
                    if (IsCancellationRequested)
                        break;
                    await Task.Run(() => tuple.Item1(tuple.Item2));
                    await Task.Delay(period);
                }

            }, Tuple.Create(callback, state), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }

        public new void Dispose() { base.Cancel(); }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            Cancel();
            start(dueTime, period);
        }
    }
}

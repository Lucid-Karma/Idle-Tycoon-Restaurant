using System;
using System.Threading.Tasks;

namespace CrazyGames
{
    public static class CrazyTaskUtil
    {
        /// <summary>
        /// Wraps a single-parameter callback (no error) into a Task.
        /// </summary>
        public static Task<T> FromCallback<T>(Action<Action<T>> register)
        {
            var tcs = new TaskCompletionSource<T>();
            register(result => tcs.SetResult(result));
            return tcs.Task;
        }

        /// <summary>
        /// Wraps an (SdkError, T) callback into a Task.
        /// Assumes SdkError is already an Exception (so no conversion/wrapping is done).
        /// </summary>
        public static Task<T> FromCallback<T>(Action<Action<SdkError, T>> register)
        {
            var tcs = new TaskCompletionSource<T>();
            register(
                (error, result) =>
                {
                    if (error != null)
                    {
                        tcs.SetException(error);
                    }
                    else
                    {
                        tcs.SetResult(result);
                    }
                }
            );
            return tcs.Task;
        }
    }
}

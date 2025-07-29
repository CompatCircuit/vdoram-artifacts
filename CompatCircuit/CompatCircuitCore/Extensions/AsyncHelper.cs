using System.Diagnostics;

namespace Anonymous.CompatCircuitCore.Extensions;
public static class AsyncHelper {
    /// <summary>
    /// This method is primarily used for debugging async methods, where unhandled exceptions are ignored.
    /// </summary>
    /// <param name="action"></param>
    public static async Task TerminateOnException(Func<Task> action) {
        try {
            await action();
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            Serilog.Log.Error(ex, ex.ToString());
            Debugger.Break();

            // Forcely terminate the program, as throwing an exception will be ignored
            Environment.Exit(1);
            throw;
        }
    }

    /// <summary>
    /// This method is primarily used for debugging async methods, where unhandled exceptions are ignored.
    /// </summary>
    /// <param name="action"></param>
    public static async Task<TResult> TerminateOnException<TResult>(Func<Task<TResult>> action) {
        try {
            return await action();
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            Serilog.Log.Error(ex, ex.ToString());
            Debugger.Break();

            // Forcely terminate the program, as throwing an exception will be ignored
            Environment.Exit(1);
            throw;
        }
    }

    /// <summary>
    /// This method is primarily used for debugging async methods, where unhandled exceptions are ignored.
    /// </summary>
    /// <param name="action"></param>
    public static async IAsyncEnumerable<TResult> TerminateOnException<TResult>(Func<IAsyncEnumerable<TResult>> action) {
        // https://stackoverflow.com/a/12060223
        IAsyncEnumerable<TResult> enumerable = action();
        IAsyncEnumerator<TResult> enumerator = enumerable.GetAsyncEnumerator();

        bool shouldContinue = true;
        while (shouldContinue) {
            try {
                shouldContinue = await enumerator.MoveNextAsync();
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) {
                Serilog.Log.Error(ex, ex.ToString());
                Debugger.Break();

                // Forcely terminate the program, as throwing an exception will be ignored
                Environment.Exit(1);
                throw;
            }

            // the yield statement is outside the try catch block
            yield return enumerator.Current;
        }
    }
}

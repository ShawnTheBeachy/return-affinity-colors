namespace ReturnColors;

internal sealed class Disposables : List<IDisposable>, IDisposable
{
    public void Dispose()
    {
        foreach (var disposable in this)
            disposable.Dispose();
    }
}

internal static class DisposableExtensions
{
    public static T DisposeWith<T>(this T disposable, Disposables disposables)
        where T : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }
}

namespace Client.Services;

public enum ToastKind { Success, Error }

public record ToastMessage(Guid Id, string Text, ToastKind Kind);

// A tiny reusable "middle layer" any component can call into: Show(...) alone for a one-off
// message, or RunAsync(...) to wrap a submit-style action with a loading flag plus a
// success/error toast — the same try/catch/finally shape every submit handler in this app
// would otherwise hand-roll individually (see MyRequests.razor's SubmitNewRequest for the
// first caller). ToastHost.razor is the one place that actually renders these.
public class ToastService
{
    private const int AutoDismissMs = 3500;

    public event Action? Changed;
    public List<ToastMessage> Active { get; } = [];

    public void Show(string text, ToastKind kind = ToastKind.Success)
    {
        var toast = new ToastMessage(Guid.NewGuid(), text, kind);
        Active.Add(toast);
        Changed?.Invoke();

        _ = DismissAfterDelayAsync(toast.Id);
    }

    // Runs action, flips setLoading around it, and shows successMessage/errorMessage (either
    // may be null to skip that toast) — callers that don't need a loading flag can pass null.
    public async Task RunAsync(Func<Task> action, Action<bool>? setLoading = null, string? successMessage = null, string? errorMessage = null)
    {
        setLoading?.Invoke(true);
        try
        {
            await action();
            if (successMessage is not null)
                Show(successMessage);
        }
        catch (Exception)
        {
            if (errorMessage is not null)
                Show(errorMessage, ToastKind.Error);
        }
        finally
        {
            setLoading?.Invoke(false);
        }
    }

    private async Task DismissAfterDelayAsync(Guid id)
    {
        await Task.Delay(AutoDismissMs);
        if (Active.RemoveAll(t => t.Id == id) > 0)
            Changed?.Invoke();
    }
}

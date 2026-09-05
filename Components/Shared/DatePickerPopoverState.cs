namespace DeveloperWorkManager.Components.Shared;

public sealed class DatePickerPopoverState
{
    private string? activePickerId;

    public event Action? Changed;

    public bool IsOpen(string pickerId) => activePickerId == pickerId;

    public void Toggle(string pickerId)
    {
        activePickerId = activePickerId == pickerId ? null : pickerId;
        Changed?.Invoke();
    }

    public void Close(string pickerId)
    {
        if (activePickerId != pickerId)
        {
            return;
        }

        activePickerId = null;
        Changed?.Invoke();
    }
}

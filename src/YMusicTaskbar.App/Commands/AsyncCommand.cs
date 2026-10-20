using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace YMusicTaskbar;

public sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute, Action<Exception> onError) : ICommand
{
    private bool running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !running && canExecute();
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        running = true; Refresh();
        try { await execute(); }
        catch (Exception exception) { onError(exception); }
        finally { running = false; Refresh(); }
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

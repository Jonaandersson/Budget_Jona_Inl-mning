#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Budget_Jona_Inlämning.Services;

public class DialogService : IDialogService
{
    public Task<bool?> ShowDialogAsync(UserControl view, object viewModel)
    {
        var tcs = new TaskCompletionSource<bool?>();

        var window = new Window
        {
            Title = viewModel?.GetType().Name ?? "Dialog",
            Content = view,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Application.Current?.MainWindow,
            ResizeMode = ResizeMode.NoResize
        };

        window.DataContext = viewModel;

        void CloseHandler(object? s, EventArgs? e)
        {
            // If the viewmodel exposes a CloseRequested event, set the dialog result accordingly
            if (viewModel is IDialogRequestClose drc)
            {
                drc.CloseRequested -= CloseHandler;
            }

            window.Close();
            // If viewModel implements IHaveDialogResult, we may set the result from it, otherwise null
            if (viewModel is IHaveDialogResult h && h.DialogResult.HasValue)
            {
                tcs.TrySetResult(h.DialogResult);
            }
            else
            {
                tcs.TrySetResult(null);
            }
        }

        if (viewModel is IDialogRequestClose requestClose)
        {
            requestClose.CloseRequested += CloseHandler;
        }

        // If window closed by user (x), complete the task
        window.Closed += (_, _) =>
        {
            if (!tcs.Task.IsCompleted)
            {
                if (viewModel is IHaveDialogResult h && h.DialogResult.HasValue)
                {
                    tcs.TrySetResult(h.DialogResult);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            }
        };

        // Show as dialog
        window.ShowDialog();

        return tcs.Task;
    }
}

// Small helper interfaces for dialog pattern
public interface IDialogRequestClose
{
    event EventHandler? CloseRequested;
}

public interface IHaveDialogResult
{
    bool? DialogResult { get; }
}
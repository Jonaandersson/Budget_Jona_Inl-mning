#nullable enable
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Budget_Jona_Inlämning.Services;

public class DialogService : IDialogService
{
    /// <summary>
    /// Displays the specified view as a modal dialog window, using the provided view model as its data context, and
    /// returns a task that completes when the dialog is closed.
    /// </summary>
    /// <remarks>If the view model implements <c>IDialogRequestClose</c>, the dialog will close when its
    /// <c>CloseRequested</c> event is raised. If the view model implements <c>IHaveDialogResult</c>, its
    /// <c>DialogResult</c> property determines the returned value. Otherwise, the result is <see langword="null"/>. The
    /// dialog is shown modally and blocks interaction with the owner window until closed.</remarks>
    /// <param name="view">The user control to display as the content of the dialog window.</param>
    /// <param name="viewModel">The view model to use as the data context for the dialog. If the view model implements dialog-related
    /// interfaces, its state may influence the dialog result.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is <see langword="true"/> if the dialog was
    /// accepted, <see langword="false"/> if it was canceled, or <see langword="null"/> if no explicit result was
    /// provided.</returns>
    public Task<bool?> ShowDialogAsync(UserControl view, object viewModel)
    {
        var tcs = new TaskCompletionSource<bool?>();

        Window window = new Window
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
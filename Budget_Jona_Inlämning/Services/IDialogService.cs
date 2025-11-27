#nullable enable
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Budget_Jona_Inlämning.Services;

public interface IDialogService
{
    Task<bool?> ShowDialogAsync(UserControl view, object viewModel);
}
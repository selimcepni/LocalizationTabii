using CommunityToolkit.Mvvm.Input;
using LocalizationTabii.Models;

namespace LocalizationTabii.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}
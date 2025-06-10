namespace LocalizationTabii.Services
{
    /// <summary>
    /// Modal Error Handler.
    /// </summary>
    public class ModalErrorHandler : IErrorHandler
    {
        SemaphoreSlim _semaphore = new(1, 1);

        /// <summary>
        /// Handle error in UI.
        /// </summary>
        /// <param name="ex">Exception.</param>
        public void HandleError(Exception ex)
        {
            DisplayAlert(ex).FireAndForgetSafeAsync();
        }

        /// <summary>
        /// Shows an error message with a title.
        /// </summary>
        /// <param name="title">Error title</param>
        /// <param name="message">Error message</param>
        public void ShowError(string title, string message)
        {
            DisplayErrorAlert(title, message).FireAndForgetSafeAsync();
        }

        async Task DisplayErrorAlert(string title, string message)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (Shell.Current is Shell shell)
                    await shell.DisplayAlert(title, message, "Tamam");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        async Task DisplayAlert(Exception ex)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (Shell.Current is Shell shell)
                    await shell.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
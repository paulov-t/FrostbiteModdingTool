using FMT.Logging;
using FrostySdk.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FMT.Controls.Controls
{
    /// <summary>
    /// Interaction logic for LoadingDialog.xaml
    /// </summary>
    public partial class LoadingDialog : UserControl, ILogger
    {
        public LoadingDialog()
        {
            InitializeComponent();
        }

        public void SetProgressBarMaximum(int maximum)
        {
            pbar.Maximum = maximum;
        }

        public void Update(int progress)
        {
            Dispatcher.Invoke(() =>
            {
                pbar.Value = progress;
            });
        }

        public async Task UpdateAsync(int progress)
        {
            await Task.Run(() => { Update(progress); });
        }

        Random randomNumber = new Random();

        public void Hide()
        {
            Update(null, null);
        }

        public void ShowWithGenericMessage()
        {
            Dispatcher.Invoke(() =>
            {

                pbar.Value = randomNumber.Next(0, 100);
                lblLoadingSubtitle.Text = "Busy. Please Wait!";
                lblProgress.Text = "";

                this.Visibility = Visibility.Visible;
            });
        }

        public void Update(string loadingSubTitle, string loadingCurrentMessage)
        {
            Dispatcher.Invoke(() =>
            {

                pbar.Value = randomNumber.Next(0, 100);
                lblLoadingSubtitle.Text = string.IsNullOrEmpty(loadingSubTitle) ? "" : loadingSubTitle;
                lblProgress.Text = string.IsNullOrEmpty(loadingCurrentMessage) ? "" : loadingCurrentMessage;

                this.Visibility = string.IsNullOrEmpty(loadingSubTitle) && string.IsNullOrEmpty(loadingCurrentMessage) ? Visibility.Collapsed : Visibility.Visible;
            });
        }

        public async Task<bool> UpdateAsync(string loadingSubTitle, string loadingCurrentMessage)
        {
            return await Task.Run(() => { Update(loadingSubTitle, loadingCurrentMessage); return true; });
        }

        public void Update(string loadingSubTitle, string loadingCurrentMessage, int progress)
        {
            Update(loadingSubTitle, loadingCurrentMessage);
            Dispatcher.Invoke(() =>
            {
                pbar.Value = progress;
            });
        }

        public async void UpdateAsync(string loadingSubTitle, string loadingCurrentMessage, int progress)
        {
            await Task.Run(() => { Update(loadingSubTitle, loadingCurrentMessage, progress); });
        }

        public void Log(string text, params object[] vars)
        {
            Dispatcher.Invoke(() =>
            {
                Update(lblLoadingSubtitle.Text.ToString(), text);
            });
        }

        public void LogWarning(string text, params object[] vars)
        {
            Update(lblLoadingSubtitle.Text.ToString(), text);
        }

        public void LogError(string text, params object[] vars)
        {
            Update(lblLoadingSubtitle.Text.ToString(), text);
        }

        public void LogProgress(int progress)
        {
            throw new NotImplementedException();
        }
    }
}

using FMT.Logging;
using FrostySdk.Interfaces;
using System.Threading.Tasks;
using System.Windows;

namespace FrostbiteModdingUI.Windows
{
    public interface IEditorWindow : ILogger
    {
        public Window OwnerWindow { get; set; }

        public Task UpdateBrowsersAllFull();

        public void UpdateAllBrowsers();

        public void ShowLoadingDialog(string message, string title, int progress);

    }
}

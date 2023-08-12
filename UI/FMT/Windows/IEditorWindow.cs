using FrostySdk.Interfaces;
using System.Threading.Tasks;
using System.Windows;

namespace FrostbiteModdingUI.Windows
{
    public interface IEditorWindow : ILogger
    {
        public Window OwnerWindow { get; set; }

        public Task UpdateAllBrowsersFull();

        public void UpdateAllBrowsers();

    }
}

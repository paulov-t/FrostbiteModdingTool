using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FrostyEditor.Windows
{
    public class SdkUpdateTask : INotifyPropertyChanged
    {
        public enum SdkUpdateTaskStage
        {
            Process = 0,
            TypeInfo = 1,
            TypeDump = 2,
            CrossReference = 3,
            CompileSdk = 4,
        }

        public delegate bool TaskDelegate(SdkUpdateTask task, object state);

        private string displayName;

        private SdkUpdateTaskState state;

        private string statusMessage;

        private string failMessage;

        public SdkUpdateTaskStage Stage;

        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
                NotifyPropertyChanged("DisplayName");
            }
        }

        public SdkUpdateTaskState State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
                NotifyPropertyChanged("State");
            }
        }

        public string StatusMessage
        {
            get
            {
                return statusMessage;
            }
            set
            {
                statusMessage = value;
                NotifyPropertyChanged("StatusMessage");
            }
        }

        public string FailMessage
        {
            get
            {
                return failMessage;
            }
            set
            {
                failMessage = value;
                NotifyPropertyChanged("FailMessage");
            }
        }

        public TaskDelegate Task
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

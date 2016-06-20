using System;
using System.Windows.Input;

namespace AlarmServer
{
    public class UICommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                if (isEnabled == value)
                    return;

                isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        bool isEnabled;
        Action executeAction;

        public UICommand(Action executeAction, bool isEnabled)
        {
            this.isEnabled = isEnabled;
            this.executeAction = executeAction;
        }

        public bool CanExecute(object parameter)
        {
            return isEnabled;
        }

        public void Execute(object parameter)
        {
            executeAction();
        }
    }
}

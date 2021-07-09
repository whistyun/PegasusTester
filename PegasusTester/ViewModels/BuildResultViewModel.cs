using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace PegasusTester.ViewModels
{
    public class BuildResultViewModel : ViewModelBase
    {
        private bool _HasError;
        public bool HasError
        {
            get => _HasError;
            set => this.RaiseAndSetIfChanged(ref _HasError, value);
        }

        private ObservableCollection<string> _Logs = new ObservableCollection<string>();
        public ObservableCollection<string> Logs
        {
            get => _Logs;
            set => this.RaiseAndSetIfChanged(ref _Logs, value);
        }
    }
}

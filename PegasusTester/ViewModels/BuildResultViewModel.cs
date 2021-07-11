using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;

namespace PegasusTester.ViewModels
{
    public delegate void ProcessCompileError(string fpath, int line, int column, string code, string message);

    public class BuildResultViewModel : ViewModelBase
    {
        private static readonly Regex DotNetMsgPtn = new Regex(
            @"^(?<path>([A-Z]:)?[^:]+?)\((?<ln>\d+),(?<col>\d+)\): *error (?<code>[A-Z0-9]+): *(?<message>.*)",
            RegexOptions.Compiled);

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

        public event ProcessCompileError? CompileErrorOccured;

        public void ClearLog()
        {
            Dispatcher.UIThread.InvokeAsync(Logs.Clear).Wait();
        }

        public void AddLog(string? message)
        {
            if (message is null) return;

            var task = Dispatcher.UIThread.InvokeAsync(() => Logs.Add(message));

            var mch = DotNetMsgPtn.Match(message);
            if (mch.Success)
            {
                var filepath = mch.Groups["path"].Value;
                var line = Int32.Parse(mch.Groups["ln"].Value);
                var column = Int32.Parse(mch.Groups["col"].Value);
                var code = mch.Groups["code"].Value;
                var logMessage = mch.Groups["message"].Value;

                CompileErrorOccured?.Invoke(filepath, line, column, code, logMessage);
            }

            task.Wait();
        }
    }
}

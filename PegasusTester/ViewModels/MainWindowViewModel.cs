using Avalonia.Threading;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegasusTester.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ProjectConfigPanelViewModel Config;
        private BuildResultViewModel Results;

        private List<ViewModelBase> _TabItems;
        public List<ViewModelBase> TabItems
        {
            get => _TabItems;
            set => this.RaiseAndSetIfChanged(ref _TabItems, value);
        }

        public MainWindowViewModel()
        {
            Config = new ProjectConfigPanelViewModel();
            Results = new BuildResultViewModel();
            _TabItems = CreateTabItems();

            Config.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(Config.PegSources):
                        TabItems = CreateTabItems();
                        break;
                }
            };
        }

        public async void RequestBuild()
        {
            var editors = TabItems.OfType<PegGrammarEditorViewModel>();

            foreach (var mdl in editors)
                mdl.Unload();

            GcCalls(10);

            await Task.Run(Build);

            foreach (var mdl in editors)
                mdl.ReOpenPegCode();
        }

        private void GcCalls(int retry)
        {
            foreach (var i in Enumerable.Range(0, retry))
            {
                GC.Collect();
                GC.WaitForFullGCApproach();
                GC.WaitForPendingFinalizers();
            }
        }

        private void Build()
        {
            var buildCommand = Config.BuildCommand;

            var cmdSepIdx = 0;
            var quoteMd = false;
            foreach (var ch in buildCommand)
            {
                if (ch == '"')
                {
                    quoteMd = !quoteMd;
                }
                else if (ch == ' ' && !quoteMd)
                {
                    break;
                }
                cmdSepIdx++;
            }

            var pinf = new ProcessStartInfo()
            {
                FileName = buildCommand.Substring(0, cmdSepIdx),
                Arguments = buildCommand.Substring(cmdSepIdx).Trim(),
                WorkingDirectory = Config.CurrentDirectory,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            var uith = Dispatcher.UIThread;

            uith.InvokeAsync(Results.Logs.Clear).Wait();

            var process = Process.Start(pinf);
            if (process is null)
            {
                Results.HasError = true;
                uith.InvokeAsync(() => "no process is started").Wait();
            }
            else
            {
                process.OutputDataReceived += (s, e) => uith.InvokeAsync(() => Results.Logs.Add(e.Data!)).Wait();
                process.BeginOutputReadLine();
                process.WaitForExit();

                Results.HasError = process.ExitCode != 0;
            }
        }


        private List<ViewModelBase> CreateTabItems()
        {
            var newItems = new List<ViewModelBase>();
            newItems.Add(Config);
            newItems.Add(Results);
            foreach (var pegsrc in Config.PegSources)
                newItems.Add(new PegGrammarEditorViewModel(this, pegsrc, Config));

            return newItems;
        }
    }
}

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
            Results.CompileErrorOccured += Results_CompileErrorOccured;
            _TabItems = CreateTabItems();

            Config.PropertyChanged += Config_PropertyChanged;
        }

        private void Config_PropertyChanged(object? s, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Config.PegSources):
                    TabItems = CreateTabItems();
                    break;
            }
        }

        private void Results_CompileErrorOccured(string fpath, int line, int column, string code, string message)
        {
            foreach (var item in TabItems.OfType<PegGrammarEditorViewModel>())
                item.RegisterCompileError(fpath, line, column, code, message);
        }

        public async void RequestBuild()
        {
            var editors = TabItems.OfType<PegGrammarEditorViewModel>();

            foreach (var mdl in editors)
                mdl.Unload();

            GcCalls(10);

            await Task.Run(Build);

            foreach (var mdl in editors)
            {
                mdl.CheckErrors();
                mdl.ReOpenPegCode();
            }
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

            Results.ClearLog();

            var process = Process.Start(pinf);
            if (process is null)
            {
                Results.HasError = true;
                Results.AddLog("no process is started");
            }
            else
            {
                process.OutputDataReceived += (s, e) => Results.AddLog(e.Data);
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

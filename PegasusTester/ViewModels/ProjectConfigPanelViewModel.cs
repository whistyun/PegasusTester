using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace PegasusTester.ViewModels
{
    public class ProjectConfigPanelViewModel : ViewModelBase
    {
        private string _CurrentDirectory = "";
        public string CurrentDirectory
        {
            get => _CurrentDirectory;
            set => this.RaiseAndSetIfChanged(ref _CurrentDirectory, value);
        }

        private string _ProjectFilePath = "";
        public string ProjectFilePath
        {
            get => _ProjectFilePath;
            set
            {
                this.RaiseAndSetIfChanged(ref _ProjectFilePath, value);
                SolveAssembly();
            }
        }

        private string _AssemblyPath = "";
        public string AssemblyPath
        {
            get => _AssemblyPath;
            set => this.RaiseAndSetIfChanged(ref _AssemblyPath, value);
        }

        private string _BuildCommand = "dotnet build";
        public string BuildCommand
        {
            get => _BuildCommand;
            set => this.RaiseAndSetIfChanged(ref _BuildCommand, value);
        }

        private List<string> _PegSources = new List<string>();
        public List<string> PegSources
        {
            get => _PegSources;
            set => this.RaiseAndSetIfChanged(ref _PegSources, value);
        }

        private void SolveAssembly()
        {
            if (ProjectFilePath == null) return;
            if (!File.Exists(ProjectFilePath)) return;

            File.ReadAllText(ProjectFilePath);

            var newAssemblyPath = AssemblyPath;

            var targetFramework = "";
            var newAssemblyExt = "";
            var newPegSources = new List<string>();

            using var stream = new FileStream(ProjectFilePath, FileMode.Open);
            using var reader = XmlReader.Create(stream);
            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element) continue;

                switch (reader.LocalName)
                {
                    case "TargetFramework":
                        targetFramework = reader.ReadString()?.Trim() ?? "";
                        break;

                    case "TargetFrameworks":
                        targetFramework = reader.ReadString()?.Trim()?.Split(';')?[0] ?? "";
                        break;


                    case "OutputType":
                        switch (reader.ReadString()?.Trim()?.ToLower() ?? "")
                        {
                            case "exe":
                            case "winexe":
                                newAssemblyExt = ".exe";
                                break;

                            case "library":
                            case "module":
                                newAssemblyExt = ".dll";
                                break;
                        }
                        break;

                    case "PegGrammar":
                        reader.MoveToAttribute("Include");
                        newPegSources.Add(reader.ReadContentAsString());
                        break;
                }
            }

            newAssemblyExt = newAssemblyExt != "" ? newAssemblyExt : ".*";

            var projectNm = Path.GetFileNameWithoutExtension(ProjectFilePath);
            var projectDir = Path.GetDirectoryName(ProjectFilePath)!;
            var assemblyDir = Path.Combine(projectDir, "bin", "Debug", targetFramework);

            if (!Directory.Exists(assemblyDir))
                assemblyDir = Path.GetDirectoryName(assemblyDir)!;


            var assemblyFile = Path.Combine(assemblyDir, projectNm + newAssemblyExt);
            if (!File.Exists(assemblyFile))
            {
                while (assemblyDir != projectDir && assemblyDir != null)
                {
                    string[] exts = { ".dll", ".exe" };

                    assemblyFile = Directory.GetFiles(assemblyDir, projectNm + newAssemblyExt)
                                            .OrderBy(f => exts.Contains(Path.GetExtension(f).ToLower()) ? 0 : 1)
                                            .FirstOrDefault();

                    if (assemblyFile != null) break;

                    assemblyDir = Path.GetDirectoryName(assemblyDir);
                }
            }

            CurrentDirectory = projectDir;
            AssemblyPath = assemblyFile ?? AssemblyPath;
            PegSources = newPegSources;
        }
    }
}

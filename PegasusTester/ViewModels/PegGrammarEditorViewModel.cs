using Pegasus.Common;
using PegasusTester.Behaviors;
using PegasusTester.JsonConvert;
using PegasusTester.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PegasusTester.ViewModels
{
    public class PegGrammarEditorViewModel : ViewModelBase, IDisposable
    {
        MainWindowViewModel Owner;
        ProjectConfigPanelViewModel Config;
        DisposableParser? Parser;

        private string _Header = "No Title";
        public string Header
        {
            get => _Header;
            set => this.RaiseAndSetIfChanged(ref _Header, value);
        }

        private string _PegFilePath = "";
        public string PegFilePath
        {
            get => _PegFilePath;
            set => this.RaiseAndSetIfChanged(ref _PegFilePath, value);
        }

        private List<ErrorInfo> _BuildErrorInfos = new List<ErrorInfo>();
        public List<ErrorInfo> BuildErrorInfos
        {
            get => _BuildErrorInfos;
            set => this.RaiseAndSetIfChanged(ref _BuildErrorInfos, value);
        }

        private string _PegCode = "";
        public string PegCode
        {
            get => _PegCode;
            set
            {
                this.RaiseAndSetIfChanged(ref _PegCode, value);
                IsDirty = true;
            }
        }

        private Task? _ParseTask;
        private string _ParseeInput = "";
        public string ParseeInput
        {
            get => _ParseeInput;
            set
            {
                this.RaiseAndSetIfChanged(ref _ParseeInput, value);

                if (_ParseTask == null || _ParseTask.IsCompleted || _ParseTask.IsFaulted)
                    _ParseTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            var oldVal = _ParseeInput;
                            Task.Delay(400);
                            if (oldVal == _ParseeInput)
                            {
                                TestCode(oldVal);
                                break;
                            }
                        }
                    });
            }
        }

        private string _ParsedResult = "";
        public string ParsedResult
        {
            get => _ParsedResult;
            set => this.RaiseAndSetIfChanged(ref _ParsedResult, value);
        }

        private CursorTreeModel[]? _Lexicals;
        public CursorTreeModel[]? Lexicals
        {
            get => _Lexicals;
            set => this.RaiseAndSetIfChanged(ref _Lexicals, value);
        }

        private bool _IsDirty;
        public bool IsDirty
        {
            get => _IsDirty;
            set => this.RaiseAndSetIfChanged(ref _IsDirty, value);
        }

        private bool _IsBad;
        public bool IsBad
        {
            get => _IsBad;
            set => this.RaiseAndSetIfChanged(ref _IsBad, value);
        }

        public bool HasMultipleParserMethod
        {
            get => (TargetParserMethodNames ?? Array.Empty<string>()).Length > 1;
        }

        private string? _SelectedParserMethodName;
        public string? SelectedParserMethodName
        {
            get => _SelectedParserMethodName;
            set
            {
                this.RaiseAndSetIfChanged(ref _SelectedParserMethodName, value);
                TestCode(ParseeInput);
            }
        }

        private string[] _targetParserMethodNames = new string[0];
        public string[] TargetParserMethodNames
        {
            get => _targetParserMethodNames;
            set
            {
                this.RaiseAndSetIfChanged(ref _targetParserMethodNames, value);
                this.RaisePropertyChanged(nameof(HasMultipleParserMethod));
            }
        }

        private string _warnMessage = "";
        public string WarnMessage
        {
            get => _warnMessage;
            set => this.RaiseAndSetIfChanged(ref _warnMessage, value);
        }

        private string _errorMessage = "";
        public string ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public PegGrammarEditorViewModel(
            MainWindowViewModel owner,
            string pegFilePath,
            ProjectConfigPanelViewModel config)
        {
            Owner = owner;
            Config = config;
            Header = Path.GetFileName(pegFilePath);
            PegFilePath = Path.Combine(config.CurrentDirectory, pegFilePath);

            var skim = PegasusSkimReader.FromPath(PegFilePath);
            _PegCode = skim.FullText;
            CreateParser(skim.FQCN);
        }

        public void RegisterCompileError(string fpath, int line, int column, string code, string message)
        {
            if (Path.GetFullPath(fpath) != Path.GetFullPath(PegFilePath))
                return;

            BuildErrorInfos.Add(new ErrorInfo(code, line, column, message));
        }

        internal void CheckErrors()
        {
            if (BuildErrorInfos.Count > 0)
            {
                // FIXME: RaisePropertyChanged has no effect to DependencyProperty...?
                BuildErrorInfos = new List<ErrorInfo>(BuildErrorInfos);
            }
        }

        public void ReOpenPegCode()
        {
            IsDirty = false;

            var skim = PegasusSkimReader.FromPath(PegFilePath);

            if (_PegCode != skim.FullText)
            {
                _PegCode = skim.FullText;
                this.RaisePropertyChanged(nameof(PegCode));
            }

            CreateParser(skim.FQCN);
        }

        private void CreateParser(string fqcn)
        {
            Parser = new DisposableParser(Config.AssemblyPath, fqcn);
            IsBad = !Parser.Ready;

            TargetParserMethodNames = Parser.TargetParserMethodNames;
            if (!TargetParserMethodNames.Contains(SelectedParserMethodName))
                SelectedParserMethodName = TargetParserMethodNames.FirstOrDefault();
        }

        public void SavePegCode()
        {
            IsDirty = false;
            File.WriteAllText(PegFilePath, PegCode);

            Owner.RequestBuild();
        }

        public void TestCode(string testSentence)
        {
            if (IsBad || Parser is null)
            {
                ErrorMessage = "Error: 'Parse' methond is not found";
                WarnMessage = "";
                ParsedResult = "";
                return;
            }

            try
            {
                ParsedResult = Parser.ParseAndConvertJson(SelectedParserMethodName, testSentence, out var lexicals);
                ErrorMessage = "";

                if (lexicals != null && lexicals.Count > 0)
                {
                    Lexicals = CursorTreeModel.Create(lexicals);

                    var lastPos = lexicals.Select(lex => lex.EndCursor)
                                          .OrderByDescending(cursor => cursor.Location)
                                          .First();

                    if (testSentence.Length > lastPos.Location)
                    {
                        WarnMessage = $"line {lastPos.Line}, column {lastPos.Column}: Parse stopped in the middle";
                    }
                    else
                    {
                        WarnMessage = "";
                    }
                }
                else
                {
                    Lexicals = null;
                }

            }
            catch (TargetInvocationException e)
            {
                var innerException = e.InnerException;
                if (innerException is FormatException fe && fe.Data.Contains("cursor") && fe.Data["cursor"] is Cursor cursor)
                {
                    ErrorMessage = $"line {cursor.Line}, column {cursor.Column}: {fe.Message}\r\n{fe.ToString()}";
                    WarnMessage = "";
                    ParsedResult = "";
                }
                else
                {
                    ErrorMessage = (e.InnerException ?? e).ToString();
                    WarnMessage = "";
                    ParsedResult = "";
                }
            }
            catch (Exception e)
            {
                ErrorMessage = e.ToString();
                WarnMessage = "";
                ParsedResult = "";
            }
        }

        public void Unload()
        {
            BuildErrorInfos = new List<ErrorInfo>();

            Parser?.Dispose();
            Parser = null;
            IsBad = true;
        }


        ~PegGrammarEditorViewModel()
        {
            Dispose();
        }

        public void Dispose() => Unload();
    }

    class DisposableParser
    {
        private AssemblyLoadContext? Loader;
        private Assembly? PegAssembly;
        private Type? ParserType;
        private object? ParserObject;
        private MethodInfo[]? TargetParserMethods;
        private MethodInfo? JsonConverter;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public DisposableParser(string targetAssembly, string targetPerserClass)
        {
            Loader = new AssemblyLoadContext("pegtstr", true);
            Loader.Resolving += (ctx, nm) =>
            {
                var asmDir = Path.GetDirectoryName(targetAssembly);

                var asmFiles = Directory.GetFiles(asmDir!, "*.*", SearchOption.AllDirectories)
                                    .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                                           || path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                foreach (var asmFile in asmFiles)
                {
                    try
                    {
                        var asm = ctx.LoadFromAssemblyPath(asmFile);
                        if (asm.FullName == nm.FullName)
                            return asm;
                    }
                    catch { }
                }

                return null;
            };

            if (File.Exists(targetAssembly))
            {
                PegAssembly = Loader.LoadFromAssemblyPath(targetAssembly);
            }

            var jsonAsm = Loader.LoadFromAssemblyPath(typeof(JsonLike).Assembly.Location);
            JsonConverter = jsonAsm.GetType("PegasusTester.JsonConvert.JsonLike")?
                                 .GetMethod("Stringify")!;

            ParserType = PegAssembly?.GetType(targetPerserClass);

            if (ParserType != null)
            {
                ParserObject = Activator.CreateInstance(ParserType);

                TargetParserMethods =
                    ParserType.GetMethods(BindingFlags.Instance | BindingFlags.Public)
                              .Where(minf => minf.Name.StartsWith("Parse"))  // Parse or Parse***
                              .Where(minf =>
                                     // code, filename
                                     IsParamMatch(minf, new[] { typeof(string), typeof(string) })
                                     // code, filename, lexicalElements
                                     || IsParamMatch(minf, new[] { typeof(string), typeof(string), typeof(IList<LexicalElement>) }))
                              .GroupBy(minf => minf.Name, (k, minfs) => minfs.OrderByDescending(minf => minf.GetParameters().Length).First())
                              .ToArray();
            }

        }

        public bool Ready
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => Check(TargetParserMethods).Length > 0;
        }

        public string[] TargetParserMethodNames
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => Check(TargetParserMethods).Select(minf => minf.Name).ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public object? Parse(string? methodName, string testcode, out IList<LexicalElement>? lexicalElements)
        {
            var method = Check(TargetParserMethods).Where(minf => minf.Name == methodName).First();

            if (method.GetParameters().Length == 2)
            {
                lexicalElements = null;
                return method?.Invoke(ParserObject, new[] { testcode, null });
            }
            else
            {
                var prms = new object?[] { testcode, null, null };
                var rslt = method?.Invoke(ParserObject, prms);

                lexicalElements = prms[2] as IList<LexicalElement>;
                return rslt;
            }
        }

        private T Check<T>(T? v)
        {
            if (v is null) throw new InvalidOperationException();
            return v;
        }

        private bool IsParamMatch(MethodInfo minf, Type[] requests)
        {
            var mparams = minf.GetParameters()
                              .Select(pinf => pinf.ParameterType)
                              .Select(ptype => ptype.GetElementType() ?? ptype)
                              .ToArray();

            if (mparams.Length != requests.Length)
                return false;

            foreach (var i in Enumerable.Range(0, mparams.Length))
            {
                if (mparams[i].IsAssignableFrom(requests[i]))
                    continue;

                if (mparams[i].FullName == requests[i].FullName)
                    continue;

                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string ParseAndConvertJson(string? methodName, string testcode, out IList<LexicalElement>? lexicalElements)
        {
            var obj = Parse(methodName, testcode, out lexicalElements);
            return JsonLike.Stringify(obj);

            //return Check(JsonConverter).Invoke(null, new[] { obj }) as string ?? "";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            PegAssembly = null;
            ParserType = null;
            ParserObject = null;
            TargetParserMethods = null;
            JsonConverter = null;
            Loader?.Unload();
            Loader = null;
        }
    }

    public class PegasusSkimReader
    {
        const string DefaultNameSpace = "Parsers";
        const string DefaultClassName = "Parser";

        public string FQCN { get; }
        public string FullText { get; }

        private PegasusSkimReader(string pegSourceText)
        {
            FullText = pegSourceText;

            var classNmMch = Regex.Match(FullText, "@classname[ \t\r\n]+([^ \t\r\n]+)");
            var nmspcMch = Regex.Match(FullText, "@namespace[ \t\r\n]+([^ \t\r\n]+)");

            var simpleClassName = classNmMch.Success ? classNmMch.Groups[1].Value : DefaultClassName;
            var @namespace = nmspcMch.Success ? nmspcMch.Groups[1].Value : DefaultNameSpace;

            FQCN = @namespace + "." + simpleClassName;
        }

        public static PegasusSkimReader FromPath(string filepath)
            => new PegasusSkimReader(File.ReadAllText(filepath));

        public static PegasusSkimReader FromCode(string pegSourceText)
            => new PegasusSkimReader(pegSourceText);
    }
}

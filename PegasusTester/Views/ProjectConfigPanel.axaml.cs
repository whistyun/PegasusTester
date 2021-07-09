using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.IO;

namespace PegasusTester.Views
{
    public partial class ProjectConfigPanel : UserControl
    {
        public ProjectConfigPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void Button_Click(object sender, RoutedEventArgs arg)
        {

            var tb = this.Find<TextBox>("ProjectFilePath");
            var initTxt = tb.Text;

            var dg = new OpenFileDialog();
            dg.Filters = new List<FileDialogFilter>(new[] {
                new FileDialogFilter() {
                    Name = ".csproj",
                    Extensions = new List<string>(new[] { "csproj" })
                } });
            dg.AllowMultiple = false;

            if (!String.IsNullOrEmpty(initTxt))
            {
                dg.Directory = Path.GetDirectoryName(initTxt) ??
                               Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dg.InitialFileName = Path.GetFileName(initTxt);
            }

            var result = await dg.ShowAsync(GetOwnerWindow());

            var len = result?.Length;
            len.GetValueOrDefault();

            if (result != null && result.Length > 0)
            {
                tb.Text = result[0];
            }
        }

        private Window? GetOwnerWindow()
        {
            var parent = this.Parent;
            while (parent != null)
            {
                if (parent is Window win) return win;
                parent = parent.Parent;
            }
            return null;
        }
    }
}

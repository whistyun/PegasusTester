using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PegasusTester.Views
{
    public partial class BuildResult : UserControl
    {
        public BuildResult()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

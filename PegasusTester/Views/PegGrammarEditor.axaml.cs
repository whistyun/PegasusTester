using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PegasusTester.Views
{
    public partial class PegGrammarEditor : UserControl
    {
        public PegGrammarEditor()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

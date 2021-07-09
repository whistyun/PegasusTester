using Avalonia;
using Avalonia.Platform;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PegasusTester.Syntax
{
    public static class AvalonEditHighlightingLoader
    {
        public static IHighlightingDefinition Load(string uri)
        {
            var loader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            using (var stream = loader.Open(new Uri(uri)))
            {
                XshdSyntaxDefinition xshd;
                using (var s = stream)
                using (var reader = XmlReader.Create(s))
                {
                    // in release builds, skip validating the built-in highlightings
                    xshd = HighlightingLoader.LoadXshd(reader);
                }
                return HighlightingLoader.Load(xshd, HighlightingManager.Instance);
            }
        }
    }
}

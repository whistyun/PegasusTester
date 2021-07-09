using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegasusTester.Views
{
    public class TypeMatchingSelector : IDataTemplate
    {
        public bool SupportsRecycling => false;
        [Content]
        public List<DataTemplate> Templates { get; } = new List<DataTemplate>();

        public IControl Build(object param)
        {
            if (param == null) return null;

            foreach (var template in Templates)
            {
                if (param.GetType() == template.DataType)
                {
                    var contro = template.Build(param);
                    contro.DataContext = param;

                    return contro;
                }
            }

            return null;
        }

        public bool Match(object data)
        {
            return true;
        }
    }
}

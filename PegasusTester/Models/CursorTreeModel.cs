using Pegasus.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegasusTester.Models
{
    public class CursorTreeModel
    {
        public CursorTreeModel? Parent { private set; get; }

        public int StartLocation { get; }
        public int StartLine { get; }
        public int StartColumn { get; }

        public int EndLocation { get; }
        public int EndLine { get; }
        public int EndColumn { get; }

        public string Name { get; }

        public List<CursorTreeModel> Items { get; }

        private CursorTreeModel(int max)
        {
            Name = "root";
            StartLocation = 0;
            EndLocation = max;
            Items = new List<CursorTreeModel>();
        }

        public CursorTreeModel(LexicalElement element)
        {
            Name = element.Name;

            StartLocation = element.StartCursor.Location;
            StartLine = element.StartCursor.Line;
            StartColumn = element.StartCursor.Column;

            EndLocation = element.EndCursor.Location;
            EndLine = element.EndCursor.Line;
            EndColumn = element.EndCursor.Column;

            Items = new List<CursorTreeModel>();
        }


        public static CursorTreeModel[] Create(IList<LexicalElement> lexicals)
        {
            var root = new CursorTreeModel(lexicals.Max(lex => lex.EndCursor.Location));

            void Restructure(CursorTreeModel child, CursorTreeModel parent)
            {
                bool isParentFound = false;

                foreach (var p in parent.Items.ToArray())
                {
                    if (p.Contains(child))
                    {
                        Restructure(child, p);
                        isParentFound = true;
                    }
                    else if (child.Contains(p))
                    {
                        child.AddChild(p);
                    }
                }

                if (!isParentFound)
                {
                    parent.AddChild(child);
                }
            }

            void Sorts(CursorTreeModel model)
            {
                model.Items.Sort((a, b) => a.StartLocation - b.StartLocation);

                foreach (var item in model.Items)
                {
                    Sorts(item);
                }
            }

            foreach (var leaf in lexicals.Reverse().Select(lx => new CursorTreeModel(lx)))
            {
                Restructure(leaf, root);
            }

            Sorts(root);

            return root.Items.ToArray();
        }

        public bool Contains(CursorTreeModel child)
        {
            return StartLocation <= child.StartLocation
                && child.EndLocation <= EndLocation;
        }


        public void AddChild(CursorTreeModel child)
        {

            if (child.Parent != null)
            {
                child.Parent.RemoveChild(child);
            }

            child.Parent = this;
            Items.Add(child);
        }

        public void RemoveChild(CursorTreeModel child)
        {
            if (child.Parent == this)
            {
                child.Parent = null;
                Items.Remove(child);
            }
        }
    }
}

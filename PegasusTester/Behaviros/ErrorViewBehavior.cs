using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PegasusTester.Behaviors
{
    public class ErrorViewBehavior : Behavior<TextEditor>, IBackgroundRenderer, ITextViewConnect
    {
        public static readonly StyledProperty<IList<ErrorInfo>?> ErrorInfosProperty =
            AvaloniaProperty.Register<ErrorViewBehavior, IList<ErrorInfo>?>(nameof(ErrorInfos));

        private TextEditor? _textEditor = null;
        private TextSegmentCollection<ErrorInfoSegument> _errorSeguments = new TextSegmentCollection<ErrorInfoSegument>();
        readonly List<TextView> _textViews = new List<TextView>();

        public IList<ErrorInfo>? ErrorInfos
        {
            get => GetValue(ErrorInfosProperty);
            set => SetValue(ErrorInfosProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            if (AssociatedObject is TextEditor textEditor)
            {
                _textEditor = textEditor;
                textEditor.TextArea.TextView.BackgroundRenderers.Add(this);
                textEditor.PointerMoved += TextEditor_PointerMoved;

                this.GetObservable(ErrorInfosProperty).Subscribe(ErrorInfosPropertyChanged);
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }


        public KnownLayer Layer
        {
            get => KnownLayer.Selection;
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));
            if (drawingContext == null)
                throw new ArgumentNullException(nameof(drawingContext));

            if (_errorSeguments.Count == 0) return;
            if (!textView.VisualLinesValid) return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0) return;

            int viewStart = visualLines.First().FirstDocumentLine.Offset;
            int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var marker in _errorSeguments.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
                    geoBuilder.AlignToWholePixels = true;
                    geoBuilder.CornerRadius = 3;
                    geoBuilder.AddSegment(textView, marker);
                    Geometry geometry = geoBuilder.CreateGeometry();
                    if (geometry != null)
                    {
                        SolidColorBrush brush = new SolidColorBrush(Colors.Red, 0.1);
                        drawingContext.DrawGeometry(brush, null, geometry);
                    }
                }
            }
        }

        public void AddToTextView(TextView textView)
        {
            if (textView != null && !_textViews.Contains(textView))
            {
                _textViews.Add(textView);
            }
        }

        public void RemoveFromTextView(TextView textView)
        {
            if (textView != null)
            {
                _textViews.Remove(textView);
            }
        }

        private void ErrorInfosPropertyChanged(IList<ErrorInfo>? infos)
        {
            if (_textEditor != null)
            {
                // oldseg
                Redraw(_errorSeguments);

                _errorSeguments.Clear();

                if (infos != null)
                {
                    foreach (var inf in infos.Select(inf => inf.CreateSegument(_textEditor)))
                        _errorSeguments.Add(inf);
                    Redraw(_errorSeguments);
                }
            }
        }

        private void Redraw(IEnumerable<ErrorInfoSegument> segs)
        {
            foreach (var view in _textViews)
            {
                foreach (var seg in segs)
                {
                    view.Redraw(seg, DispatcherPriority.Normal);
                }
            }
        }

        private ErrorInfoSegument? _oldMarker;
        private void TextEditor_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (_textEditor is null) return;

            ErrorInfoSegument? _newMarker = null;

            var textView = _textEditor.TextArea.TextView;
            foreach (var marker in _errorSeguments)
            {
                foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
                {
                    if (r.Contains(e.GetPosition(textView)))
                    {
                        _newMarker = marker;
                        goto tipshow;
                    }
                }
            }

        tipshow:
            if (_newMarker is null)
            {
                ToolTip.SetIsOpen(_textEditor, false);
            }
            else if (_newMarker != _oldMarker)
            {
                ToolTip.SetTip(_textEditor, _newMarker.Message);
                ToolTip.SetIsOpen(_textEditor, true);
            }
            _oldMarker = _newMarker;
        }
    }

    public class ErrorInfo
    {
        public string Code { get; }
        public int Line { get; }
        public int Column { get; }
        public string Message { get; }

        public ErrorInfo(string code, int line, int column, string message)
        {
            Code = code;
            Line = line;
            Column = column;
            Message = message;
        }

        public ErrorInfoSegument CreateSegument(TextEditor editor)
        {
            var location = new TextLocation(Line, Column);
            var offset = editor.TextArea.Document.GetOffset(location);
            var lastOffset = Math.Min(offset + 4, editor.TextArea.Document.TextLength);

            return new ErrorInfoSegument(offset, lastOffset, Message);
        }
    }

    public class ErrorInfoSegument : TextSegment
    {
        public string Message { get; }

        public ErrorInfoSegument(int start, int end, string message)
        {
            StartOffset = start;
            EndOffset = end;
            Length = end - start + 1;
            Message = message;
        }
    }
}

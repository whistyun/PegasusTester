using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PegasusTester.JsonConvert
{
    public static class JsonLike
    {
        public static string Stringify(object? data)
        {
            var buff = new StringBuilder();
            InternalStringify(data, 0, 0, buff);
            return buff.ToString();
        }

        private static void InternalStringify(object? data, int firstIndent, int indent, StringBuilder buffer)
        {
            if (TrySingleVal(data, out var txt))
            {
                buffer.Indent(firstIndent).Append(txt);
                return;
            }
            else if (data is IDictionary dic)
            {
                buffer.Indent(firstIndent).Append("Dictionary{").AppendLine();

                var isFirst = true;
                foreach (var key in dic.Keys)
                {
                    if (isFirst) isFirst = false;
                    else
                    {
                        buffer.Append(',').AppendLine();
                    }

                    InternalStringify(key, indent + 1, indent + 1, buffer);
                    buffer.Append(": ");
                    InternalStringify(dic[key], 0, indent + 1, buffer);
                }

                if (!isFirst) buffer.AppendLine();
                buffer.Indent(indent).Append("}");
            }
            else if (data is IEnumerable ary)
            {
                buffer.Indent(firstIndent).Append("[").AppendLine();

                var isFirst = true;
                foreach (var elm in ary)
                {
                    if (isFirst) isFirst = false;
                    else
                    {
                        buffer.Append(',').AppendLine();
                    }

                    InternalStringify(elm, indent + 1, indent + 1, buffer);
                }

                if (!isFirst) buffer.AppendLine();
                buffer.Indent(indent).Append("]");
            }
            else
            {
                var dataType = data.GetType();
                var dataProps = dataType.GetProperties()
                                   .Where(pinf => pinf.CanRead && pinf.CanWrite)
                                   .Where(pinf => pinf.GetSetMethod() != null)
                                   .Where(pinf => pinf.GetGetMethod() != null && pinf.GetGetMethod().GetParameters().Length == 0)
                                   .ToArray();

                if (dataProps.Length < 3)
                {
                    var propVals = dataProps.Select(pinf => pinf.GetValue(data)).ToArray();

                    if (propVals.All(v => TrySingleVal(v, out var _)))
                    {
                        buffer.Indent(firstIndent).Append(dataType.Name).Append('{');

                        for (var i = 0; i < propVals.Count(); ++i)
                        {
                            if (i >= 1)
                            {
                                buffer.Append(", ");
                            }
                            buffer.Append(dataProps[i].Name);
                            buffer.Append(": ");

                            TrySingleVal(propVals[i], out var valTxt);
                            buffer.Append(valTxt);
                        }

                        buffer.Append('}');
                        return;
                    }
                }

                buffer.Indent(firstIndent).Append(dataType.Name).Append('{').AppendLine();

                var isFirst = true;
                foreach (var prop in dataProps)
                {
                    if (isFirst) isFirst = false;
                    else
                    {
                        buffer.Append(',').AppendLine();
                    }

                    buffer.Indent(indent + 1).Append(prop.Name);
                    buffer.Append(": ");
                    try
                    {
                        InternalStringify(prop.GetValue(data), 0, indent + 1, buffer);
                    }
                    catch
                    {
                        buffer.Append("#Error#");
                    }
                }

                if (!isFirst) buffer.AppendLine();
                buffer.Indent(indent).Append("}");
            }
        }



        private static bool TrySingleVal(object? data, out string outTxt)
        {
            string? text;

            if (data == null)
                text = "null";
            else if (data is bool)
                text = data.ToString();
            else if (data is byte)
                text = data.ToString();
            else if (data is sbyte)
                text = data.ToString();
            else if (data is decimal)
                text = data.ToString();
            else if (data is double)
                text = data.ToString();
            else if (data is float)
                text = data.ToString();
            else if (data is int)
                text = data.ToString();
            else if (data is uint)
                text = data.ToString();
            else if (data is nint)
                text = data.ToString();
            else if (data is nuint)
                text = data.ToString();
            else if (data is long)
                text = data.ToString();
            else if (data is ulong)
                text = data.ToString();
            else if (data is short)
                text = data.ToString();
            else if (data is ushort)
                text = data.ToString();
            else if (data is char)
                text = "'" + data.ToString() + "'";
            else if (data is string)
                text = "\"" + data + "\"";
            else if (data is Guid)
                text = "\"" + data.ToString() + "\"";
            else if (data is DateTime)
                text = "\"" + data.ToString() + "\"";
            else
            {
                outTxt = "";
                return false;
            }

            outTxt = text ?? "null";
            return true;
        }
    }

    internal static class StringExt
    {
        public static StringBuilder Indent(this StringBuilder tgt, int count)
        {
            tgt.Append(new String(' ', count * 4));
            return tgt;
        }
    }
}
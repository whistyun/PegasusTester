using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
using Avalonia.Data;
using Avalonia;
using DynamicExpresso;

namespace PegasusTester.Behaviors
{
    public class ExpressoBinding : IBinding
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content, AssignBinding]
        public IDictionary<string, IBinding> Bindings { get; set; } = new Dictionary<string, IBinding>();

        public string? Formula { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding result is null.
        /// </summary>
        public object TargetNullValue { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; } = BindingMode.OneWay;

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource? RelativeSource { get; set; }

        public ExpressoBinding()
        {
            FallbackValue = AvaloniaProperty.UnsetValue;
            TargetNullValue = AvaloniaProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            var targetType = targetProperty?.PropertyType ?? typeof(object);

            var kvlist = Bindings.Select(kv => Tuple.Create(kv.Key, kv.Value));

            var keyList = kvlist.Select(tpl => tpl.Item1).ToList();

            var input = kvlist.Select(tpl => tpl.Item2.Initiate(target, null).Observable)
                              .CombineLatest()
                              .Select(x => EvaluteValue(keyList, x, targetType, Formula))
                              .Where(x => x != BindingOperations.DoNothing);

            return InstancedBinding.OneWay(input, Priority);
        }


        private object EvaluteValue(IList<string> names, IList<object?> values, Type targetType, string? formula)
        {
            if (formula is null)
                return values;


            // check weither value has error
            foreach (var val in values)
            {
                if (val is BindingNotification not
                        && not.ErrorType != BindingErrorType.None)
                {
                    return values;
                }
            }

            var interpreter = new Interpreter();
            for (var i = 0; i < names.Count; ++i)
                interpreter.SetVariable(names[i], values[i]);

            try
            {
                return interpreter.Eval(formula, targetType);
            }
            catch(Exception e)
            {
                return values;
            }
        }
    }
}

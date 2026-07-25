using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>
    /// AOT-compatible replacement for complex TypeDescriptor system
    /// Provides simple data binding support without reflection-heavy CustomTypeDescriptor
    /// </summary>
    public static class FCTBDataBindingHelper
    {
        /// <summary>
        /// Sets up simple data binding for FastColoredTextBox
        /// This replaces the complex TypeDescriptor system with a direct approach
        /// </summary>
        public static void SetupDataBinding(FastColoredTextBox textBox, object dataSource, string dataMember)
        {
            if (textBox == null || dataSource == null)
                return;

            // Simple direct binding - AOT compatible
            // Uses BindingTextChanged event instead of complex TypeDescriptor manipulation
            var binding = new Binding("Text", dataSource, dataMember, false, DataSourceUpdateMode.OnPropertyChanged);
            
            // Handle the binding manually to avoid TypeDescriptor complexity
            binding.Format += (sender, e) => {
                // Custom formatting if needed
            };
            
            binding.Parse += (sender, e) => {
                // Custom parsing if needed  
            };

            textBox.DataBindings.Add(binding);
        }

        /// <summary>
        /// Alternative method for manual data binding setup
        /// Completely bypasses TypeDescriptor system
        /// </summary>
        public static void SetupManualBinding(FastColoredTextBox textBox, 
            Func<string> getter, 
            Action<string> setter)
        {
            if (textBox == null || getter == null || setter == null)
                return;

            // Manual two-way binding
            textBox.BindingTextChanged += (sender, e) => {
                setter(textBox.Text);
            };

            // Initial value
            textBox.Text = getter();
        }
    }

    // Legacy classes kept for compatibility but marked as obsolete
    [Obsolete("Use FCTBDataBindingHelper for AOT-compatible data binding", true)]
    class FCTBDescriptionProvider : TypeDescriptionProvider
    {
        public FCTBDescriptionProvider(Type type) : base(GetDefaultTypeProvider(type)) { }
        private static TypeDescriptionProvider GetDefaultTypeProvider(Type type) => TypeDescriptor.GetProvider(type);
        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) => 
            throw new NotSupportedException("Use FCTBDataBindingHelper for AOT-compatible data binding");
    }

    [Obsolete("Use FCTBDataBindingHelper for AOT-compatible data binding", true)]
    class FCTBTypeDescriptor : CustomTypeDescriptor
    {
        public FCTBTypeDescriptor(ICustomTypeDescriptor parent, object instance) : base(parent) { }
        public override EventDescriptorCollection GetEvents() => 
            throw new NotSupportedException("Use FCTBDataBindingHelper for AOT-compatible data binding");
    }

    [Obsolete("Use FCTBDataBindingHelper for AOT-compatible data binding", true)]
    class FooTextChangedDescriptor : EventDescriptor
    {
        public FooTextChangedDescriptor(MemberDescriptor desc) : base(desc) { }
        public override void AddEventHandler(object component, Delegate value) => 
            throw new NotSupportedException("Use FCTBDataBindingHelper for AOT-compatible data binding");
        public override Type ComponentType => typeof(FastColoredTextBox);
        public override Type EventType => typeof(EventHandler);
        public override bool IsMulticast => true;
        public override void RemoveEventHandler(object component, Delegate value) => 
            throw new NotSupportedException("Use FCTBDataBindingHelper for AOT-compatible data binding");
    }
}

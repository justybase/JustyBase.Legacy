using System.ComponentModel;
using System.Windows.Forms;

namespace JustData.Mvvm;

internal static class BindingSourceExtensions
{
    public static Binding BindOnPropertyChanged(
        this BindingSource bindingSource,
        BindableComponent control,
        string controlProperty,
        string viewModelProperty)
    {
        ArgumentNullException.ThrowIfNull(bindingSource);
        ArgumentNullException.ThrowIfNull(control);

        var binding = new Binding(
            controlProperty,
            bindingSource,
            viewModelProperty,
            formattingEnabled: true,
            DataSourceUpdateMode.OnPropertyChanged);

        control.DataBindings.Add(binding);
        return binding;
    }
}

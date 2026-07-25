using System.ComponentModel;
using System.Windows.Forms;

namespace JustData.Mvvm;

internal sealed class NotifyDataErrorInfoErrorProvider : IDisposable
{
    private readonly ErrorProvider _errorProvider;
    private readonly INotifyDataErrorInfo _source;
    private readonly IReadOnlyDictionary<string, Control> _controls;

    public NotifyDataErrorInfoErrorProvider(
        ErrorProvider errorProvider,
        INotifyDataErrorInfo source,
        IReadOnlyDictionary<string, Control> controls)
    {
        _errorProvider = errorProvider;
        _source = source;
        _controls = controls;
        _source.ErrorsChanged += OnErrorsChanged;

        foreach (var propertyName in _controls.Keys)
        {
            Update(propertyName);
        }
    }

    public void Dispose() => _source.ErrorsChanged -= OnErrorsChanged;

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e) => Update(e.PropertyName);

    private void Update(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_controls.TryGetValue(propertyName, out var control))
        {
            return;
        }

        var errors = _source.GetErrors(propertyName)?.Cast<object>().Select(error => error?.ToString()).Where(error => !string.IsNullOrWhiteSpace(error));
        _errorProvider.SetError(control, errors is null ? string.Empty : string.Join(Environment.NewLine, errors));
    }
}

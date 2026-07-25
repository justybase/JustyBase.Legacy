using CommunityToolkit.Mvvm.ComponentModel;

namespace JustData.ViewModels;

/// <summary>Base class for view models that expose validation errors to the view.</summary>
public abstract class ViewModelBase : ObservableValidator
{
}

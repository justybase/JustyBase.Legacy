namespace JustyBaseLegacy.UI
{
    public partial class BaseWindow
    {
        private bool _startupSchemaRefreshStarted;

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_startupSchemaRefreshStarted)
            {
                return;
            }

            _startupSchemaRefreshStarted = true;
            try
            {
                await Task.Yield();

                string connectionName = CurrentUpper?.SelectedConnectionName ?? SelectedConnectionName;
                if (string.IsNullOrEmpty(connectionName)
                    || _generalDbService.DriverName(connectionName) != "NetezzaSQL")
                {
                    return;
                }

                _completionRuntimeContext.SelectedConnectionName = connectionName;
                await CbConnectionsSelectedIndexChanged(enabled => CurrentUpper?.SetEnabledConnectionsDatabases(enabled));
            }
            catch (OperationCanceledException)
            {
                // Startup refresh can be superseded by shutdown or a later session action.
            }
            catch (Exception exception)
            {
                _loggerLoud.LogError("Startup schema refresh failed", exception);
                SchemaRefreshOptionEnable(true);
            }
        }
    }
}

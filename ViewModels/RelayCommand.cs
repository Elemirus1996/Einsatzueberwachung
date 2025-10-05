using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Einsatzueberwachung.Services;

namespace Einsatzueberwachung.ViewModels
{
    /// <summary>
    /// RelayCommand-Implementation für MVVM-Pattern
    /// Erweitert mit Parameter-Support und Async-Funktionalität
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action? _execute;
        private readonly Action<object?>? _executeWithParameter;
        private readonly Func<bool>? _canExecute;
        private readonly Func<object?, bool>? _canExecuteWithParameter;
        private readonly Func<Task>? _executeAsync;

        // Standard Command ohne Parameter
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Command mit object-Parameter
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _executeWithParameter = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteWithParameter = canExecute;
        }

        // Async Command ohne Parameter
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            try
            {
                if (_canExecuteWithParameter != null)
                    return _canExecuteWithParameter(parameter);
                
                return _canExecute?.Invoke() ?? true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in CanExecute", ex);
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                if (_executeAsync != null)
                {
                    // Async execution - fire and forget für UI-Commands
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _executeAsync();
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.LogError("Error in async command execution", ex);
                        }
                    });
                }
                else if (_executeWithParameter != null)
                {
                    _executeWithParameter(parameter);
                }
                else
                {
                    _execute?.Invoke();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError("Error in command execution", ex);
            }
        }

        /// <summary>
        /// Async-Ausführung für explizite async-Calls
        /// </summary>
        public async Task ExecuteAsync()
        {
            if (_executeAsync != null)
            {
                await _executeAsync();
            }
            else
            {
                Execute(null);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Strongly-typed RelayCommand mit Parameter-Support
    /// </summary>
    /// <typeparam name="T">Parameter-Type</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?>? _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            try
            {
                if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                    return false;

                T? convertedParameter = ConvertParameter(parameter);
                return _canExecute?.Invoke(convertedParameter) ?? true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in CanExecute<{typeof(T).Name}>", ex);
                return false;
            }
        }

        public void Execute(object? parameter)
        {
            try
            {
                T? convertedParameter = ConvertParameter(parameter);
                _execute?.Invoke(convertedParameter);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.LogError($"Error in Execute<{typeof(T).Name}>", ex);
            }
        }

        private T? ConvertParameter(object? parameter)
        {
            if (parameter == null)
                return default(T);
            
            if (parameter is T directParameter)
                return directParameter;
            
            try
            {
                return (T?)Convert.ChangeType(parameter, typeof(T));
            }
            catch
            {
                LoggingService.Instance.LogWarning($"Could not convert parameter {parameter?.GetType().Name} to {typeof(T).Name}");
                return default(T);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

// Copyright (C) Explorer++ Project
// SPDX-License-Identifier: GPL-3.0-only
// See LICENSE in the top level directory

using System;
using System.Windows.Input;

namespace ExplorerPlusPlus.WinUIHost.Infrastructure
{
	public sealed class RelayCommand : ICommand
	{
		private readonly Action m_execute;
		private readonly Func<bool>? m_canExecute;

		public event EventHandler? CanExecuteChanged;

		public RelayCommand(Action execute, Func<bool>? canExecute = null)
		{
			m_execute = execute;
			m_canExecute = canExecute;
		}

		public bool CanExecute(object? parameter)
		{
			return m_canExecute?.Invoke() ?? true;
		}

		public void Execute(object? parameter)
		{
			m_execute();
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public sealed class RelayCommand<T> : ICommand
	{
		private readonly Action<T?> m_execute;
		private readonly Predicate<T?>? m_canExecute;

		public event EventHandler? CanExecuteChanged;

		public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
		{
			m_execute = execute;
			m_canExecute = canExecute;
		}

		public bool CanExecute(object? parameter)
		{
			if (m_canExecute == null)
			{
				return true;
			}

			if (parameter is T typedParameter)
			{
				return m_canExecute(typedParameter);
			}

			if (parameter == null)
			{
				return m_canExecute(default);
			}

			return false;
		}

		public void Execute(object? parameter)
		{
			if (parameter is T typedParameter)
			{
				m_execute(typedParameter);
				return;
			}

			m_execute(default);
		}

		public void NotifyCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CoffeeShop.ViewModel
{
	public class ManageAccountViewModel : INotifyPropertyChanged
	{
		private readonly CoffeeDbContext _context;
		public List<bool?> Genders { get; } = new List<bool?> { true, false, null };
		public List<int> Types { get; } = new List<int> { 0, 1 };

		private List<Account> _accounts;
		public List<Account> Accounts
		{
			get { return _accounts; }
			set
			{
				_accounts = value;
				OnPropertyChanged();
			}
		}

		private Account _selectedAccount;
		public Account SelectedAccount
		{
			get => _selectedAccount;
			set
			{
				_selectedAccount = value;
				if (value != null)
				{
					EditingAccount = new Account
					{
						Id = value.Id,
						Name = value.Name,
						Password = value.Password,
						Gender = value.Gender,
						Type = value.Type,
						IsDeleted = value.IsDeleted
					};
				}
				else
				{
					EditingAccount = new Account();
				}
				OnPropertyChanged(); OnPropertyChanged(nameof(EditingAccount));
			}
		}

		private Account _editingAccount;
		public Account EditingAccount
		{
			get => _editingAccount;
			set
			{
				_editingAccount = value;
				OnPropertyChanged();
			}
		}

		public ICommand AddCommand { get; }
		public ICommand UpdateCommand { get; }
		public ICommand DeleteCommand { get; }
		public ICommand NewAccountCommand { get; }

		public ManageAccountViewModel()
		{
			_context = new CoffeeDbContext();
			AddCommand = new RelayCommand(AddAccount, CanAddAccount);
			UpdateCommand = new RelayCommand(UpdateAccount, CanUpdateAccount);
			DeleteCommand = new RelayCommand(DeleteAccount, CanDeleteAccount);
			NewAccountCommand = new RelayCommand(PrepareNewAccount);
			LoadAccounts();
			EditingAccount = new Account();
		}

		private void LoadAccounts()
		{
			Accounts = _context.Accounts.ToList();
		}

		private void PrepareNewAccount()
		{
			SelectedAccount = null;
			EditingAccount = new Account();
		}

		private void AddAccount()
		{
			EditingAccount.IsDeleted = false;
			_context.Accounts.Add(EditingAccount);
			_context.SaveChanges();
			LoadAccounts();
			SelectedAccount = EditingAccount;
			EditingAccount = new Account();
		}

		private bool CanAddAccount()
		{
			return !string.IsNullOrWhiteSpace(EditingAccount.Name) && EditingAccount.Id == 0;
		}

		private void UpdateAccount()
		{
			if (SelectedAccount == null) return;

			SelectedAccount.Name = EditingAccount.Name;
			SelectedAccount.Password = EditingAccount.Password;
			SelectedAccount.Gender = EditingAccount.Gender;
			SelectedAccount.Type = EditingAccount.Type;
			SelectedAccount.IsDeleted = EditingAccount.IsDeleted;
			_context.Entry(SelectedAccount).State = EntityState.Modified;
			_context.SaveChanges();
			LoadAccounts();
		}

		private bool CanUpdateAccount()
		{
			return SelectedAccount != null && !string.IsNullOrWhiteSpace(EditingAccount.Name);
		}

		private void DeleteAccount()
		{
			if (SelectedAccount == null) return;

			SelectedAccount.IsDeleted = true;
			_context.Entry(SelectedAccount).State = EntityState.Modified;
			_context.SaveChanges();
			LoadAccounts();
			SelectedAccount = null;
		}

		private bool CanDeleteAccount()
		{
			return SelectedAccount != null;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
	public class GenderConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool?)
			{
				bool? genderValue = (bool?)value;
				return genderValue;
			}
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool?)
			{
				bool? genderValue = (bool?)value;
				return genderValue;
			}
			return null;
		}
	}

	public class TypeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int typeValue)
			{
				return typeValue;
			}
			return -1;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is int typeValue)
			{
				return typeValue;
			}
			return -1;
		}
	}

	public static class PasswordBoxBehavior
	{
		public static readonly DependencyProperty PasswordProperty =
			DependencyProperty.RegisterAttached("Password", typeof(string), typeof(PasswordBoxBehavior),
				new FrameworkPropertyMetadata(string.Empty, OnPasswordChanged));

		public static string GetPassword(DependencyObject d)
		{
			return (string)d.GetValue(PasswordProperty);
		}

		public static void SetPassword(DependencyObject d, string value)
		{
			d.SetValue(PasswordProperty, value);
		}

		private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is PasswordBox passwordBox)
			{
				passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
				passwordBox.Password = (string)e.NewValue;
				passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
			}
		}

		private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
		{
			if (sender is PasswordBox passwordBox)
			{
				SetPassword(passwordBox, passwordBox.Password);
			}
		}
	}
}

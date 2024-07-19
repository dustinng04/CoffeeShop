using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Model.Models;

namespace CoffeeShop.ViewModel
{
	public class LoginWindowViewModel : INotifyPropertyChanged
	{
		private readonly CoffeeDbContext _context;
		private string _username;
		public string Username
		{
			get => _username;
			set
			{
				_username = value;
				OnPropertyChanged(nameof(Username));
			}
		}

		private string _password;
		public string Password
		{
			get => _password;
			set
			{
				_password = value;
				OnPropertyChanged(nameof(Password));
			}
		}

		public ICommand LoginCommand { get; }

		public event PropertyChangedEventHandler PropertyChanged;

		public LoginWindowViewModel()
		{
			_context = new CoffeeDbContext();
			LoginCommand = new RelayCommand(Login, CanLogin);
		}

		private bool CanLogin()
		{
			return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
		}

		private void Login()
		{
			if (Username == "admin" && Password == "admin123")
			{
				var account = new Account { Name = Username, Type = 0 }; // Admin
				OpenMainWindow(account);
			}
			else if (Username == "staff" && Password == "staff123")
			{
				var account = _context.Accounts.FirstOrDefault(x => x.Name == Username);
				OpenMainWindow(account);
			}
			else
			{
				MessageBox.Show("Invalid username or password", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OpenMainWindow(Account account)
		{
			var mainWindow = new MainWindow();
			if (mainWindow.DataContext is MainViewModel mainViewModel)
			{
				mainViewModel.CurrentAccount = account;
			}
			else
			{
				MessageBox.Show("MainViewModel not found in DataContext", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			mainWindow.Show();

			// Close the login window
			Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault()?.Close();
		}

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}

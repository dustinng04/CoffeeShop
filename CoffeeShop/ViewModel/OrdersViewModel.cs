using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CoffeeShop.ViewModel
{
	public class OrdersViewModel : INotifyPropertyChanged
	{
		private readonly CoffeeDbContext _context;
		private List<TableFood> _tableStatusList;
		public List<TableFood> TableStatusList
		{
			get { return _tableStatusList; }
			set
			{
				_tableStatusList = value;
				OnPropertyChanged("TableStatusList");
			}
		}

		public List<Food> FoodList { get; set; }
		public List<Food> DrinkList { get; set; }

		private Food _selectedFood;
		public Food SelectedFood
		{
			get => _selectedFood;
			set
			{
				_selectedFood = value;
				OnPropertyChanged();
			}
		}

		private Food _selectedDrink;
		public Food SelectedDrink
		{
			get => _selectedDrink;
			set
			{
				_selectedDrink = value;
				OnPropertyChanged();
			}
		}

		private int _itemQuantity = 1;
		public int ItemQuantity
		{
			get => _itemQuantity;
			set
			{
				_itemQuantity = value;
				OnPropertyChanged();
			}
		}
		//private int _discountAmount;
		//public int DiscountAmount { get => _discountAmount; set { _discountAmount = value; OnPropertyChanged(); } }

		public Account CurrentAccount { get;}

		public ICommand AddItemCommand { get;}
		public ICommand MoveTableCommand { get; }
		public ICommand CheckoutCommand { get;}

		public OrdersViewModel(Account currentAccount)
		{
			CurrentAccount = currentAccount;
			_context = new CoffeeDbContext();
			LoadTables();
			LoadFoodAndDrinks();
			AddItemCommand = new RelayCommand(AddItem);
			MoveTableCommand = new RelayCommand(MoveTable);
			CheckoutCommand = new RelayCommand(Checkout);
		}

		public void LoadTables()
		{
			TableStatusList = _context.TableFoods.ToList();
			AvailableTables = _context.TableFoods.Where(t => t.Status == true).ToList();
			OnPropertyChanged(nameof(AvailableTables));
		}

		private void LoadFoodAndDrinks()
		{
			FoodList = _context.Foods.Where(f => f.IdCategory == 1).ToList();
			DrinkList = _context.Foods.Where(f => f.IdCategory == 2).ToList();
		}

		private TableFood _selectedTable;
		public TableFood SelectedTable
		{
			get => _selectedTable;
			set
			{
				_selectedTable = value;
				OnPropertyChanged();
				LoadOrderDetails();
			}
		}
		public ObservableCollection<BillInfoViewModel> OrderDetails { get; set; } = new ObservableCollection<BillInfoViewModel>();

		private void LoadOrderDetails()
		{
			if (SelectedTable == null)
			{
				OrderDetails.Clear();
				return;
			}

			var orderDetails = GetBillInfoForTable(SelectedTable.Id);

			OrderDetails.Clear();
			foreach (var detail in orderDetails)
			{
				OrderDetails.Add(detail);
			}
		}

		public List<BillInfoViewModel> GetBillInfoForTable(int tableId)
		{
			// Most recent bill
			var mostRecentBill = _context.Bills
				.Where(b => b.IdTable == tableId && b.IdTableNavigation.Status == false)
				.OrderByDescending(b => b.DateCheckIn)
				.FirstOrDefault();

			if (mostRecentBill != null)
			{
				var result = _context.BillInfos
					.Where(bi => bi.IdBill == mostRecentBill.Id)
					.Include(bi => bi.IdFoodNavigation)
					.Select(bi => new BillInfoViewModel
					{
						foodImg = bi.IdFoodNavigation.ImageLink,
						FoodName = bi.IdFoodNavigation.Name,
						Quantity = bi.Quantity,
						Price = bi.IdFoodNavigation.Price
					})
					.ToList();

				return result;
			}
			else
			{
				return new List<BillInfoViewModel>(); // Return an empty list if no active bills are found
			}
		}

		private void Checkout()
		{
			//if (DiscountAmount < 0 || DiscountAmount > 20)
			//{
			//	MessageBox.Show("Discount amount must be between 0 and 20 percent.");
			//	return;
			//}

			var transaction = _context.Database.BeginTransaction();
			try
			{
				// Find the active bill for the selected table
				var activeBill = _context.Bills
							.Include(b => b.BillInfos).ThenInclude(bi => bi.IdFoodNavigation)
							.Where(b => b.IdTable == SelectedTable.Id && b.IdTableNavigation.Status == false)
							.OrderByDescending(b => b.DateCheckIn)
							.FirstOrDefault();

                if (activeBill == null)
				{
					MessageBox.Show("No active bill found for the selected table.");
					return;
				}

				// Calculate total price
				double totalPrice = activeBill.BillInfos.Sum(bi => bi.Quantity * bi.IdFoodNavigation.Price);

				// Apply discount
				//double discountAmount = totalPrice * (DiscountAmount / 100);
				//double finalPrice = totalPrice - discountAmount;

				// Update the bill
				activeBill.DateCheckOut = DateTime.Now;
				activeBill.Status = true; // Bill is now paid
				//activeBill.TotalPrice = finalPrice;

				// Update table status
				SelectedTable.Status = true; // Table is now available

				_context.SaveChanges();
				transaction.Commit();

				MessageBox.Show($"Checkout successful!\n" +
								$"Subtotal: {totalPrice:C2}\n" +
								//$"Discount: {discountAmount:C2} ({DiscountAmount}%)\n" +
								$"Total: {totalPrice:C2}");

				// Refresh the table list and clear order details
				LoadTables();
				OrderDetails.Clear();
				SelectedTable = null;
				//DiscountAmount = 0;
			}
			catch (Exception ex)
			{
				transaction.Rollback();
				MessageBox.Show($"Error during checkout: {ex.Message}");
			}
		}

        private void AddItem()
        {
            if (SelectedTable == null)
            {
                MessageBox.Show("Please select a table first.");
                return;
            }
            if (SelectedFood == null && SelectedDrink == null)
            {
                MessageBox.Show("Please select at least one food or drink item.");
                return;
            }
            if (ItemQuantity <= 0)
            {
                MessageBox.Show("Quantity must be greater than zero.");
                return;
            }

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    Bill bill;
                    if (SelectedTable.Status) // Table is available, create new order
                    {
                        bill = new Bill
                        {
                            DateCheckIn = DateTime.Now,
                            DateCheckOut = null,
                            IdTable = SelectedTable.Id,
                            Status = false, // Bill is not paid yet
                            IssuerId = CurrentAccount.Id
                        };
                        _context.Bills.Add(bill);
                        _context.SaveChanges();
                        SelectedTable.Status = false; // Table is now occupied
                        _context.SaveChanges();
                    }
                    else // Table is occupied, find existing order
                    {
                        bill = _context.Bills
                                .Where(b => b.IdTable == SelectedTable.Id && b.IdTableNavigation.Status == false)
                                .OrderByDescending(b => b.DateCheckIn)
                                .FirstOrDefault();
                        if (bill == null)
                        {
                            MessageBox.Show("Error: No active bill found for this table.");
                            return;
                        }
                    }

                    // Function to add item to bill
                    void AddItemToBill(Food item)
                    {
                        var existingBillInfo = _context.BillInfos
                            .FirstOrDefault(bi => bi.IdBill == bill.Id && bi.IdFood == item.Id);
                        if (existingBillInfo != null)
                        {
                            existingBillInfo.Quantity += ItemQuantity;
                        }
                        else
                        {
                            var newBillInfo = new BillInfo
                            {
                                IdBill = bill.Id,
                                IdFood = item.Id,
                                Quantity = ItemQuantity
                            };
                            _context.BillInfos.Add(newBillInfo);
                        }
                    }

                    // Add selected food if any
                    if (SelectedFood != null)
                    {
                        AddItemToBill(SelectedFood);
                    }

                    // Add selected drink if any
                    if (SelectedDrink != null)
                    {
                        AddItemToBill(SelectedDrink);
                    }

                    _context.SaveChanges();
                    transaction.Commit();
                    LoadOrderDetails(); // Refresh the order details
                    MessageBox.Show("Item(s) added successfully.");

                    // Reset selections and quantity
                    SelectedFood = null;
                    SelectedDrink = null;
                    ItemQuantity = 1;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Error adding item(s): {ex.Message}");
                }
            }
        }

        public List<TableFood> AvailableTables { get; set; }

		private TableFood _moveTableNumber;
		public TableFood MoveTableNumber
		{
			get => _moveTableNumber;
			set
			{
				_moveTableNumber = value;
				OnPropertyChanged();
			}
		}

		private void MoveTable()
		{
			if (SelectedTable == null)
			{
				MessageBox.Show("Please select a table to move from.");
				return;
			}

			if (MoveTableNumber == null)
			{
				MessageBox.Show("Please select a table to move to.");
				return;
			}

			if (SelectedTable.Id == MoveTableNumber.Id)
			{
				MessageBox.Show("Cannot move to the same table.");
				return;
			}

			var result = MessageBox.Show($"Are you sure you want to move from Table {SelectedTable.Name} to Table {MoveTableNumber.Name}?",
										 "Confirm Table Move",
										 MessageBoxButton.YesNo,
										 MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				var transaction = _context.Database.BeginTransaction();
				try
				{
					// Find the bill for the current table
					var currentBill = _context.Bills
						.Where(b => b.IdTable == SelectedTable.Id && b.IdTableNavigation.Status == false)
						.OrderByDescending(b => b.DateCheckIn)
						.FirstOrDefault();

					if (currentBill == null)
					{
						MessageBox.Show("No active bill found for the selected table.");
						return;
					}

					// Update the bill with the new table ID
					currentBill.IdTable = MoveTableNumber.Id;

					// Update the status of both tables
					SelectedTable.Status = true; // Now available
					MoveTableNumber.Status = false; // Now occupied

					_context.SaveChanges();
					transaction.Commit();

					MessageBox.Show($"Successfully moved from Table {SelectedTable.Name} to Table {MoveTableNumber.Name}.");

					// Refresh the table list and order details
					LoadTables();
					LoadOrderDetails();

					// Reset selections
					SelectedTable = MoveTableNumber;
					MoveTableNumber = null;
				}
				catch (Exception ex)
				{
					transaction.Rollback();
					MessageBox.Show($"Error moving table: {ex.Message}");
				}
				
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

	}
}

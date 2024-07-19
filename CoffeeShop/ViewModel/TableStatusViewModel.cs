using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CoffeeShop.ViewModel
{
    public class TableStatusViewModel : INotifyPropertyChanged
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

        public TableStatusViewModel()
        {
            _context = new CoffeeDbContext();
            LoadTables();
        }

        public void LoadTables()
        {
            TableStatusList = _context.TableFoods.ToList();
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class BoolToStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is bool status)
            {
                return status ? "Available" : "Unavailable";
            }
            return "Unknown";
        }
        

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

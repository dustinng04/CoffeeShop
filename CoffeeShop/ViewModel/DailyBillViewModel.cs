using Microsoft.EntityFrameworkCore;
using Model.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CoffeeShop.ViewModel
{
    public class DailyBillViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<BillWithTotal> _bills;
        public ObservableCollection<BillWithTotal> Bills
        {
            get { return _bills; }
            set
            {
                _bills = value;
                OnPropertyChanged(nameof(Bills));
            }
        }

        private readonly CoffeeDbContext _context;
        public ICommand ExportCommand { get; set; }

        private string _titleText;
        public string TitleText
        {
            get => _titleText;
            set
            {
                if (_titleText != value)
                {
                    _titleText = value;
                    OnPropertyChanged(nameof(TitleText));
                }
            }
        }

        public DailyBillViewModel()
        {
            _context = new CoffeeDbContext();
            ExportCommand = new RelayCommand(Export);
            LoadDailyBills();
            UpdateTitleText();
        }

        private void LoadDailyBills()
        {
            var today = DateTime.Today;
            var bills = _context.Bills
                        .Include(b => b.BillInfos)
                            .ThenInclude(bi => bi.IdFoodNavigation)
                        .Where(b => b.DateCheckIn.Date == today)
                        .ToList();

            var billsWithTotal = bills.Select(b => new BillWithTotal
            {
                Bill = b,
                TotalPrice = b.BillInfos.Sum(bi => bi.Quantity * bi.IdFoodNavigation.Price)
            });

            Bills = new ObservableCollection<BillWithTotal>(billsWithTotal);
        }

        private void UpdateTitleText()
        {
            TitleText = $"Daily Bills - {DateTime.Today:d}";
        }

        private void Export()
        {
            // Implement export functionality here
            throw new NotImplementedException();
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

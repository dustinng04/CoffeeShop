using Microsoft.EntityFrameworkCore;
using Model.Models;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO;

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
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            string fileName = $"Daily   BillsReport.xlsx";
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, fileName);
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Bills");

                // Add title
                worksheet.Cells[1, 1].Value = TitleText;
                worksheet.Cells[1, 1, 1, 6].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Size = 14;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Add date
                worksheet.Cells[2, 1].Value = $"Generated on: {DateTime.Now.ToShortDateString()}";
                worksheet.Cells[2, 1, 2, 6].Merge = true;

                // Add headers
                string[] headers = new string[] { "ID", "Checkin", "Checkout", "Food Details", "Price", "Created By" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[4, i + 1].Value = headers[i];
                    worksheet.Cells[4, i + 1].Style.Font.Bold = true;
                }

                // Add data
                int row = 5;
                foreach (var bill in Bills)
                {
                    worksheet.Cells[row, 1].Value = bill.Bill.Id;
                    worksheet.Cells[row, 2].Value = bill.Bill.DateCheckIn;
                    worksheet.Cells[row, 3].Value = bill.Bill.DateCheckOut;
                    // Format the cells as dates
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "yyyy-mm-dd hh:mm:ss";

                    worksheet.Cells[row, 4].Value = string.Join("\n ", bill.Bill.BillInfos.Select(bi => $"{bi.IdFoodNavigation?.Name ?? "Unknown"} x{bi.Quantity}"));
                    worksheet.Cells[row, 4].Style.WrapText = true;
                    worksheet.Cells[row, 5].Value = bill.TotalPrice;
                    worksheet.Cells[row, 6].Value = bill.Bill.IssuerId;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Save the file
                try
                {
                    File.WriteAllBytes(filePath, package.GetAsByteArray());
                    MessageBox.Show($"File saved successfully at: {filePath}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

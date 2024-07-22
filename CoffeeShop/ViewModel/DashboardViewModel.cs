using Microsoft.EntityFrameworkCore;
using Model.Models;
using OxyPlot;

using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeShop.ViewModel
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        public PlotModel BarchartModel { get; set; }

        private readonly CoffeeDbContext _context;

        public PlotModel PiechartModel { get; set; }
        public PlotModel LinechartModel {  get; set; }

        private int _staffCount;
        private int _totalOrders;
        private double _totalRevenue;

        public int StaffCount
        {
            get => _staffCount;
            set
            {
                _staffCount = value;
                OnPropertyChanged(nameof(StaffCount));
            }
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set
            {
                _totalOrders = value;
                OnPropertyChanged(nameof(TotalOrders));
            }
        }

        public double TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged(nameof(TotalRevenue));
            }
        }

        public DashboardViewModel()
        {
            _context = new CoffeeDbContext();
            LoadData();
            BarchartModel = CreateBarChartModel();
            PiechartModel = CreatePieChartModel();
            LinechartModel = CreateLineChartModel();
        }

        private void LoadData()
        {
            StaffCount = _context.Accounts.Where(a => a.Type == 0 && (a.IsDeleted == false || a.IsDeleted == null)).Count();
            TotalOrders = _context.Bills.Count();
            TotalRevenue = _context.Bills
                                 .Include(b => b.BillInfos) 
                                    .ThenInclude(bi => bi.IdFoodNavigation) 
                                 .SelectMany(b => b.BillInfos)
                                 .Sum(bi => bi.Quantity * bi.IdFoodNavigation.Price);
        }

        private PlotModel? CreateBarChartModel()
        {
            // Chart config
            var model = new PlotModel
            {
                Title = "Beverages and Drinks",
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0
            };


            // Create CategoryAxis for X-axis (months)
            var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Month" };
            categoryAxis.Labels.Add("6/2024");
            categoryAxis.Labels.Add("7/2024");
            model.Axes.Add(categoryAxis);

            // Create LinearAxis for Y-axis (counts)
            var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Count" };
            model.Axes.Add(valueAxis);

            // 2 series F&B
            var beverageSeries = new ColumnSeries { Title = "Beverages", FillColor = OxyColors.Aqua };
            var drinkSeries = new ColumnSeries { Title = "Drinks", FillColor = OxyColors.Brown };

            // Count the numbers in June and July
            var orders = _context.Bills.Include(b => b.BillInfos).ThenInclude(bi => bi.IdFoodNavigation);
            var juneBeverageCount = orders
                .Where(o => o.DateCheckIn.Month == 6 && o.DateCheckIn.Year == 2024)
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 2)
                .Sum(b => b.Quantity);
            var julyBeverageCount = orders
                .Where(o => o.DateCheckIn.Month == 7 && o.DateCheckIn.Year == 2024)
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 2)
                .Sum(b => b.Quantity);
            var juneDrinkCount = orders
                .Where(o => o.DateCheckIn.Month == 6 && o.DateCheckIn.Year == 2024)
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 1)
                .Sum(b => b.Quantity);
            var julyDrinkCount = orders
                .Where(o => o.DateCheckIn.Month == 7 && o.DateCheckIn.Year == 2024)
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 1)
                .Sum(b => b.Quantity);

            // Add items to series
            beverageSeries.Items.Add(new ColumnItem { Value = juneBeverageCount });
            beverageSeries.Items.Add(new ColumnItem { Value = julyBeverageCount });
            drinkSeries.Items.Add(new ColumnItem { Value = juneDrinkCount });
            drinkSeries.Items.Add(new ColumnItem { Value = julyDrinkCount });

            model.Series.Add(beverageSeries);
            model.Series.Add(drinkSeries);

            return model;
        }

        private PlotModel CreatePieChartModel()
        {
            var model = new PlotModel
            {
                Title = "Total Beverages and Drinks",
                IsLegendVisible = true,
                LegendPosition = LegendPosition.RightTop,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Vertical,
                LegendBorderThickness = 0
            };

            var series = new PieSeries
            {
                StrokeThickness = 2.0,
                InsideLabelPosition = 0.8,
                AngleSpan = 360,
                StartAngle = 0
            };

            var orders = _context.Bills.Include(b => b.BillInfos).ThenInclude(bi => bi.IdFoodNavigation);

            var beverageCount = orders
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 2)
                .Sum(b => b.Quantity);

            var drinkCount = orders
                .SelectMany(o => o.BillInfos)
                .Where(b => b.IdFoodNavigation.IdCategory == 1)
                .Sum(b => b.Quantity);

            series.Slices.Add(new PieSlice("Beverages", beverageCount) { Fill = OxyColors.Aqua });
            series.Slices.Add(new PieSlice("Drinks", drinkCount) { Fill = OxyColors.Brown });

            model.Series.Add(series);

            return model;
        }

        private PlotModel CreateLineChartModel()
        {
            var model = new PlotModel
            {
                Title = "Orders by Times",
                LegendPosition = LegendPosition.TopRight,
                LegendPlacement = LegendPlacement.Outside,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0
            };

            // Add padding to the plot area
            model.Padding = new OxyThickness(10, 10, 10, 10);

            // Create CategoryAxis for X-axis (time ranges)
            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time Range",
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                AxislineColor = OxyColors.Black
            };
            var timeRanges = new[] { "9-12h", "12-15h", "15-18h", "18-20h", "20-24h" };
            categoryAxis.Labels.AddRange(timeRanges);
            model.Axes.Add(categoryAxis);

            // Create LinearAxis for Y-axis (order counts)
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Number of Orders",
                AbsoluteMinimum = 0,
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                AxislineColor = OxyColors.Black
            };
            model.Axes.Add(valueAxis);

            var series = new LineSeries
            {
                Title = "Orders",
                Color = OxyColors.Blue,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.Blue,
                MarkerFill = OxyColors.Blue,
                InterpolationAlgorithm = InterpolationAlgorithms.CatmullRomSpline // This makes the line curved
            };

            var orders = _context.Bills;

            var orderCounts = new[]
            {
                orders.Count(o => o.DateCheckIn.Hour >= 9 && o.DateCheckIn.Hour < 12),
                orders.Count(o => o.DateCheckIn.Hour >= 12 && o.DateCheckIn.Hour < 15),
                orders.Count(o => o.DateCheckIn.Hour >= 15 && o.DateCheckIn.Hour < 18),
                orders.Count(o => o.DateCheckIn.Hour >= 18 && o.DateCheckIn.Hour < 20),
                orders.Count(o => o.DateCheckIn.Hour >= 20 && o.DateCheckIn.Hour < 24)
            };

            for (int i = 0; i < orderCounts.Length; i++)
            {
                series.Points.Add(new DataPoint(i, orderCounts[i]));
            }
            model.Series.Add(series);

            return model;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

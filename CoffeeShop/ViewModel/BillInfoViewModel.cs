using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoffeeShop.ViewModel
{
    public class BillInfoViewModel
    {
        public string foodImg { get; set; }
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public double TotalAmount => Price * Quantity;
    }
}

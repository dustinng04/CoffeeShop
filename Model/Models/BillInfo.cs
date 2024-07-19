using System;
using System.Collections.Generic;

namespace Model.Models;

public partial class BillInfo
{
    public int Id { get; set; }

    public int IdBill { get; set; }

    public int IdFood { get; set; }

    public int Quantity { get; set; }

    public virtual Bill IdBillNavigation { get; set; } = null!;

    public virtual Food IdFoodNavigation { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Model.Models;

public partial class Bill
{
    public int Id { get; set; }

    public DateTime DateCheckIn { get; set; }

    public DateTime? DateCheckOut { get; set; }

    public int IdTable { get; set; }

    public int? IssuerId { get; set; }

    public bool Status { get; set; }

    public virtual ICollection<BillInfo> BillInfos { get; set; } = new List<BillInfo>();

    public virtual TableFood IdTableNavigation { get; set; } = null!;

    public virtual Account? Issuer { get; set; }
}

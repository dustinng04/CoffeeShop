using System;
using System.Collections.Generic;

namespace Model.Models;

public partial class Account
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool? Gender { get; set; }

    public int Type { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Bill> Bills { get; set; } = new List<Bill>();
}

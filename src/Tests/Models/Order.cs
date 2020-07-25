using System;
using System.ComponentModel.DataAnnotations.Schema;
using LanguageExt;

namespace RZ.Linq.RelationalDatabase.Tests.Models
{
    [Record]
    public partial class Order
    {
        public readonly string OrderId;
        public readonly int OwnerId;
        public readonly decimal PaidAmount;
        public readonly Option<DateTime> TargetDate;
        public readonly DateTime Created;
    }

    [Record]
    [Table("order_detail")]
    public partial class OrderLineItem
    {
        public readonly int Id;
        public readonly string ProductId;
        public readonly decimal Quantity;
        public readonly string Unit;
    }
}
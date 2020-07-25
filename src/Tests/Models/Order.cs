using System;
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
}
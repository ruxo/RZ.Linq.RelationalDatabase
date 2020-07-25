using System;
using System.ComponentModel.DataAnnotations.Schema;
using LanguageExt;

namespace RZ.Linq.RelationalDatabase.Tests.Models
{
    [Record]
    public partial class Product
    {
        public readonly string Id;
        public readonly string Name;
        [Column("created_at")] public readonly DateTime Created;
    }
}
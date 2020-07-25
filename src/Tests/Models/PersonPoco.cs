using System;

namespace RZ.Linq.RelationalDatabase.Tests.Models
{
    public sealed class PersonPoco
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
    }
}
using System;
using System.Collections.Generic;

namespace ExcelServiceModel
{
    public class ExcelSession
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public DateTime Created { get; set; }
        public List<string> Users { get; set; }
        public string WorkbookName { get; set; }
        public bool UseCalculationChain { get; set; }

        public bool WatchAllFormulaCells { get; set; }
        public bool WatchAllNames { get; set; }

        public string[] WatchCells { get; set; }
        public string[] WatchNames { get; set; }

        public ExcelSession()
        {
            WatchCells = new string[0];
            WatchNames = new string[0];
        }

        public ExcelSession(ExcelSession session)
        {
            Id = session.Id;
            Name = session.Name;
            Owner = session.Owner;
            Created = session.Created;
            Users = session.Users;
            WorkbookName = session.WorkbookName;
            WatchAllFormulaCells = session.WatchAllFormulaCells;
            WatchAllNames = session.WatchAllNames;
            WatchCells = session.WatchCells;
            WatchNames = session.WatchNames;
            UseCalculationChain = session.UseCalculationChain;
        }

        
    }
}

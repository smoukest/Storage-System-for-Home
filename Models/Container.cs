using System;
using System.Collections.ObjectModel;

namespace ApartmentInventory.Models
{
    public class Container
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int RoomId { get; set; }

        public Room Room { get; set; }
        public ObservableCollection<Item> Items { get; set; }

        public Container()
        {
            Items = new ObservableCollection<Item>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

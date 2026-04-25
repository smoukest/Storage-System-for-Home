using System;
using System.Collections.ObjectModel;

namespace ApartmentInventory.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ObservableCollection<Container> Containers { get; set; }
        public ObservableCollection<Item> Items { get; set; }

        public Room()
        {
            Containers = new ObservableCollection<Container>();
            Items = new ObservableCollection<Item>();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

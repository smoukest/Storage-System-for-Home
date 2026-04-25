using System;

namespace ApartmentInventory.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public int RoomId { get; set; }
        public int? ContainerId { get; set; }
        public string LocationInRoom { get; set; }

        public Room Room { get; set; }
        public Container Container { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

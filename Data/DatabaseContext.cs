using System;
using System.Linq;
using System.Collections.ObjectModel;
using Npgsql;
using ApartmentInventory.Models;

namespace ApartmentInventory.Data
{
    public class DatabaseContext
    {
        private readonly string _connectionString = "Server=localhost;Port=5432;Username=postgres;Password=123;Database=apartment_inventory;";

        public void Initialize()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("Database connected successfully");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
            }
        }

        public ObservableCollection<Room> GetAllRooms()
        {
            var rooms = new ObservableCollection<Room>();
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id, name FROM Rooms ORDER BY name";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var room = new Room
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"]
                                };
                                rooms.Add(room);
                            }
                        }
                    }
                }

                // Загружаем контейнеры и предметы для каждой комнаты
                foreach (var room in rooms)
                {
                    LoadContainersForRoom(room);
                    LoadItemsForRoom(room);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading rooms: {ex.Message}");
            }
            return rooms;
        }

        public void LoadContainersForRoom(Room room)
        {
            if (room == null || room.Containers == null) return;

            try
            {
                var allContainers = new System.Collections.Generic.List<Container>();
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        // Пытаемся выбрать parent_container_id, если столбец существует, иначе выбираться не будет
                        // Для совместимости мы сначала проверяем, есть ли столбец
                        bool hasParentId = false;
                        using (var checkCmd = connection.CreateCommand())
                        {
                            checkCmd.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name='containers' AND column_name='parent_container_id';";
                            var result = checkCmd.ExecuteScalar();
                            if (result != null) hasParentId = true;
                        }

                        if (hasParentId)
                        {
                            cmd.CommandText = "SELECT id, name, room_id, parent_container_id FROM Containers WHERE room_id = @roomId ORDER BY name";
                        }
                        else
                        {
                            cmd.CommandText = "SELECT id, name, room_id FROM Containers WHERE room_id = @roomId ORDER BY name";
                        }

                        cmd.Parameters.AddWithValue("@roomId", room.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int? parentId = null;
                                if (hasParentId && reader["parent_container_id"] != DBNull.Value)
                                    parentId = (int)reader["parent_container_id"];

                                allContainers.Add(new Container
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    RoomId = (int)reader["room_id"],
                                    ParentContainerId = parentId,
                                    Room = room
                                });
                            }
                        }
                    }
                }

                // Строим иерархию контейнеров
                var dict = new System.Collections.Generic.Dictionary<int, Container>();
                foreach (var c in allContainers)
                {
                    dict[c.Id] = c;
                }

                foreach (var c in Enumerable.Reverse(allContainers)) // Reverse to modify safely or just a normal loop
                {
                    if (c.ParentContainerId.HasValue && dict.ContainsKey(c.ParentContainerId.Value))
                    {
                        var parent = dict[c.ParentContainerId.Value];
                        c.ParentContainer = parent;
                        parent.ChildContainers.Add(c);
                    }
                    else
                    {
                        room.Containers.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading containers: {ex.Message}");
            }
        }

        public void LoadItemsForContainer(Container container)
        {
            if (container == null || container.Items == null) return;

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id, name, type, description, location_in_room, container_id, room_id FROM Items WHERE container_id = @containerId ORDER BY name";
                        cmd.Parameters.AddWithValue("@containerId", container.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var roomId = reader["room_id"];
                                container.Items.Add(new Item
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    ItemType = (string)reader["type"],
                                    Description = (string)reader["description"],
                                    LocationInRoom = (string)reader["location_in_room"],
                                    ContainerId = (int)reader["container_id"],
                                    RoomId = roomId == DBNull.Value ? 0 : (int)roomId,
                                    Container = container,
                                    Room = container.Room
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading items: {ex.Message}");
            }
        }

        public void LoadItemsForRoom(Room room)
        {
            if (room == null) return;

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    // Загружаем предметы во всех контейнерах рекурсивно
                    LoadItemsForContainersRecursively(room.Containers);

                    // Загружаем предметы БЕЗ контейнера (room.Items) - теперь с фильтрацией по room_id
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id, name, type, description, location_in_room, container_id, room_id FROM Items " +
                                        "WHERE container_id IS NULL AND room_id = @roomId ORDER BY name";
                        cmd.Parameters.AddWithValue("@roomId", room.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                room.Items.Add(new Item
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    ItemType = (string)reader["type"],
                                    Description = (string)reader["description"],
                                    LocationInRoom = (string)reader["location_in_room"],
                                    ContainerId = null,
                                    RoomId = room.Id,
                                    Room = room
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading room items: {ex.Message}");
            }
        }

        private void LoadItemsForContainersRecursively(System.Collections.Generic.IEnumerable<Container> containers)
        {
            if (containers == null) return;

            foreach (var container in containers)
            {
                LoadItemsForContainer(container);
                if (container.ChildContainers != null && container.ChildContainers.Count > 0)
                {
                    LoadItemsForContainersRecursively(container.ChildContainers);
                }
            }
        }

        public void AddRoom(string name)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO Rooms (name) VALUES (@name)";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding room: {ex.Message}");
            }
        }

        public void UpdateRoom(int id, string name)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Rooms SET name = @name WHERE id = @id";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating room: {ex.Message}");
            }
        }

        public void DeleteRoom(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Rooms WHERE id = @id";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting room: {ex.Message}");
            }
        }

        public void AddContainer(string name, int roomId, int? parentContainerId = null)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    bool hasParentId = false;
                    using (var checkCmd = connection.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT column_name FROM information_schema.columns WHERE table_name='containers' AND column_name='parent_container_id';";
                        var result = checkCmd.ExecuteScalar();
                        if (result != null) hasParentId = true;
                    }

                    if (!hasParentId && parentContainerId.HasValue)
                    {
                        // Добавляем таблицу если нету
                        using (var alterCmd = connection.CreateCommand())
                        {
                            alterCmd.CommandText = "ALTER TABLE Containers ADD COLUMN IF NOT EXISTS parent_container_id INT REFERENCES Containers(id) ON DELETE CASCADE;";
                            alterCmd.ExecuteNonQuery();
                        }
                        hasParentId = true;
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        if (hasParentId)
                        {
                            cmd.CommandText = "INSERT INTO Containers (name, room_id, parent_container_id) VALUES (@name, @roomId, @parentId)";
                            cmd.Parameters.AddWithValue("@parentId", parentContainerId.HasValue ? (object)parentContainerId.Value : DBNull.Value);
                        }
                        else
                        {
                            cmd.CommandText = "INSERT INTO Containers (name, room_id) VALUES (@name, @roomId)";
                        }

                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@roomId", roomId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding container: {ex.Message}");
            }
        }

        public void UpdateContainer(int id, string name)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Containers SET name = @name WHERE id = @id";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating container: {ex.Message}");
            }
        }

        public void DeleteContainer(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Containers WHERE id = @id";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting container: {ex.Message}");
            }
        }

        public void AddItem(string name, string itemType, string description, int roomId, int? containerId, string locationInRoom)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO Items (name, type, description, location_in_room, container_id, room_id) VALUES (@name, @type, @description, @location, @containerId, @roomId)";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@type", itemType ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@location", locationInRoom ?? "");
                        cmd.Parameters.AddWithValue("@containerId", (object)containerId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@roomId", roomId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding item: {ex.Message}");
            }
        }

        public void UpdateItem(int id, string name, string itemType, string description, int roomId, int? containerId, string locationInRoom)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Items SET name = @name, type = @type, description = @description, location_in_room = @location, container_id = @containerId, room_id = @roomId WHERE id = @id";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@type", itemType ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@location", locationInRoom ?? "");
                        cmd.Parameters.AddWithValue("@containerId", (object)containerId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@roomId", roomId);
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating item: {ex.Message}");
            }
        }

        public void DeleteItem(int id)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM Items WHERE id = @id";
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting item: {ex.Message}");
            }
        }
    }
}

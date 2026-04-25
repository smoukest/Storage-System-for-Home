using System;
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
                                rooms.Add(new Room
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"]
                                });
                            }
                        }
                    }
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
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT id, name, room_id FROM Containers WHERE room_id = @roomId ORDER BY name";
                        cmd.Parameters.AddWithValue("@roomId", room.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                room.Containers.Add(new Container
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    RoomId = (int)reader["room_id"]
                                });
                            }
                        }
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
                        cmd.CommandText = "SELECT id, name, type, description, location_in_room, container_id FROM Items WHERE container_id = @containerId ORDER BY name";
                        cmd.Parameters.AddWithValue("@containerId", container.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                container.Items.Add(new Item
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    ItemType = (string)reader["type"],
                                    Description = (string)reader["description"],
                                    LocationInRoom = (string)reader["location_in_room"],
                                    ContainerId = (int)reader["container_id"]
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
            var items = new ObservableCollection<Item>();

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT i.id, i.name, i.type, i.description, i.location_in_room, i.container_id FROM Items i " +
                                        "JOIN Containers c ON i.container_id = c.id WHERE c.room_id = @roomId ORDER BY i.name";
                        cmd.Parameters.AddWithValue("@roomId", room.Id);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new Item
                                {
                                    Id = (int)reader["id"],
                                    Name = (string)reader["name"],
                                    ItemType = (string)reader["type"],
                                    Description = (string)reader["description"],
                                    LocationInRoom = (string)reader["location_in_room"],
                                    ContainerId = (int)reader["container_id"]
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

        public void AddContainer(string name, int roomId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO Containers (name, room_id) VALUES (@name, @roomId)";
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
                        cmd.CommandText = "INSERT INTO Items (name, type, description, location_in_room, container_id) VALUES (@name, @type, @description, @location, @containerId)";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@type", itemType ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@location", locationInRoom ?? "");
                        cmd.Parameters.AddWithValue("@containerId", (object)containerId ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding item: {ex.Message}");
            }
        }

        public void UpdateItem(int id, string name, string itemType, string description, int? containerId, string locationInRoom)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE Items SET name = @name, type = @type, description = @description, location_in_room = @location, container_id = @containerId WHERE id = @id";
                        cmd.Parameters.AddWithValue("@name", name ?? "");
                        cmd.Parameters.AddWithValue("@type", itemType ?? "");
                        cmd.Parameters.AddWithValue("@description", description ?? "");
                        cmd.Parameters.AddWithValue("@location", locationInRoom ?? "");
                        cmd.Parameters.AddWithValue("@containerId", (object)containerId ?? DBNull.Value);
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

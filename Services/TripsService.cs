using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public class TripsService : ITripsService
    {
        private readonly IConfiguration _config;

        public TripsService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IEnumerable<TripDTO>> GetAllTripsAsync()
        {
            var trips = new Dictionary<int, TripDTO>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
                FROM Trip t
                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                JOIN Country c ON ct.IdCountry = c.IdCountry", conn);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(0);
                if (!trips.ContainsKey(id))
                {
                    trips[id] = new TripDTO
                    {
                        IdTrip = id,
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        Countries = new List<string>()
                    };
                }
                trips[id].Countries.Add(reader.GetString(6));
            }

            return trips.Values;
        }

        public async Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId)
        {
            var list = new List<ClientTripDTO>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var exists = new SqlCommand("SELECT 1 FROM Client WHERE IdClient=@id", conn);
            exists.Parameters.AddWithValue("@id", clientId);
            if ((await exists.ExecuteScalarAsync()) == null) return null;

            var cmd = new SqlCommand(@"
                SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, ct.RegisteredAt, ct.PaymentDate
                FROM Client_Trip ct
                JOIN Trip t ON ct.IdTrip = t.IdTrip
                WHERE ct.IdClient = @id", conn);
            cmd.Parameters.AddWithValue("@id", clientId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ClientTripDTO
                {
                    IdTrip = reader.GetInt32(0),
                    TripName = reader.GetString(1),
                    Description = reader.GetString(2),
                    DateFrom = reader.GetDateTime(3),
                    DateTo = reader.GetDateTime(4),
                    RegisteredAt = new DateTime(reader.GetInt64(5)),
                    PaymentDate = reader.IsDBNull(6) ? null : new DateTime?(new DateTime(reader.GetInt64(6)))
                });
            }
            return list;
        }

        public async Task<int> CreateClientAsync(ClientDTO client)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                OUTPUT INSERTED.IdClient
                VALUES (@f, @l, @e, @t, @p)", conn);

            cmd.Parameters.AddWithValue("@f", client.FirstName);
            cmd.Parameters.AddWithValue("@l", client.LastName);
            cmd.Parameters.AddWithValue("@e", client.Email);
            cmd.Parameters.AddWithValue("@t", client.Telephone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@p", client.Pesel ?? (object)DBNull.Value);

            return (int)await cmd.ExecuteScalarAsync();
        }

        public async Task<bool> RegisterClientToTrip(int clientId, int tripId)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var checkCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient=@c; SELECT 1 FROM Trip WHERE IdTrip=@t", conn);
            checkCmd.Parameters.AddWithValue("@c", clientId);
            checkCmd.Parameters.AddWithValue("@t", tripId);

            using var reader = await checkCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync() || reader.GetValue(0) == DBNull.Value) return false;
            await reader.NextResultAsync();
            if (!await reader.ReadAsync() || reader.GetValue(0) == DBNull.Value) return false;
            await reader.CloseAsync();

            var cmd = new SqlCommand("INSERT INTO Client_Trip VALUES (@c, @t, @r, NULL)", conn);
            cmd.Parameters.AddWithValue("@c", clientId);
            cmd.Parameters.AddWithValue("@t", tripId);
            cmd.Parameters.AddWithValue("@r", DateTime.Now.Ticks);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UnregisterClientFromTrip(int clientId, int tripId)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var cmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @c AND IdTrip = @t", conn);
            cmd.Parameters.AddWithValue("@c", clientId);
            cmd.Parameters.AddWithValue("@t", tripId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
    }
}
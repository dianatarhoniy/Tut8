using Tutorial8.Models.DTOs;

namespace Tutorial8.Services
{
    public interface ITripsService
    {
        Task<IEnumerable<TripDTO>> GetAllTripsAsync();
        Task<IEnumerable<ClientTripDTO>> GetClientTripsAsync(int clientId);
        Task<int> CreateClientAsync(ClientDTO client);
        Task<bool> RegisterClientToTrip(int clientId, int tripId);
        Task<bool> UnregisterClientFromTrip(int clientId, int tripId);
    }
}
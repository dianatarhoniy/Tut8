using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [ApiController]
    [Route("api")]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _service;

        public TripsController(ITripsService service)
        {
            _service = service;
        }

        [HttpGet("trips")]
        public async Task<IActionResult> GetAllTrips() => Ok(await _service.GetAllTripsAsync());

        [HttpGet("clients/{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            var result = await _service.GetClientTripsAsync(id);
            return result == null ? NotFound("Client not found") : Ok(result);
        }

        [HttpPost("clients")]
        public async Task<IActionResult> CreateClient([FromBody] ClientDTO client)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var newId = await _service.CreateClientAsync(client);
            return CreatedAtAction(nameof(GetClientTrips), new { id = newId }, newId);
        }

        [HttpPut("clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClient(int id, int tripId)
        {
            var result = await _service.RegisterClientToTrip(id, tripId);
            return result ? Ok("Registered") : NotFound("Client or trip not found");
        }

        [HttpDelete("clients/{id}/trips/{tripId}")]
        public async Task<IActionResult> UnregisterClient(int id, int tripId)
        {
            var result = await _service.UnregisterClientFromTrip(id, tripId);
            return result ? Ok("Unregistered") : NotFound("Registration not found");
        }
    }
}
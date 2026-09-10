using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDataStoreService _dataStore;

        public DashboardController(IDataStoreService dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            var summary = await _dataStore.GetDashboardSummaryAsync();
            return Ok(summary);
        }
    }
}

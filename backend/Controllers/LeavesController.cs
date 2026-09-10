using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeavesController : ControllerBase
    {
        private readonly IDataStoreService _dataStore;

        public LeavesController(IDataStoreService dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeaveRequest>>> GetLeaves(
            [FromQuery] string? employeeId,
            [FromQuery] string? status)
        {
            var leaves = await _dataStore.GetLeaveRequestsAsync(employeeId, status);
            return Ok(leaves);
        }

        [HttpPost]
        public async Task<ActionResult<LeaveRequest>> CreateLeave([FromBody] CreateLeaveRequest req)
        {
            try
            {
                var result = await _dataStore.CreateLeaveRequestAsync(req);
                return CreatedAtAction(nameof(GetLeaves), new { id = result.Id }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/approval")]
        public async Task<ActionResult<LeaveRequest>> ProcessApproval(string id, [FromBody] LeaveApprovalRequest req)
        {
            var updated = await _dataStore.ProcessLeaveRequestAsync(id, req.Approved, req.ManagerComment);
            if (updated == null) return NotFound(new { message = "Leave request not found" });
            return Ok(updated);
        }

        [HttpPut("{id}/cancel")]
        public async Task<ActionResult<LeaveRequest>> CancelLeave(string id)
        {
            var updated = await _dataStore.CancelLeaveRequestAsync(id);
            if (updated == null) return NotFound(new { message = "Leave request not found" });
            return Ok(updated);
        }
    }
}

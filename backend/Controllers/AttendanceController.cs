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
    public class AttendanceController : ControllerBase
    {
        private readonly IDataStoreService _dataStore;

        public AttendanceController(IDataStoreService dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetAttendance(
            [FromQuery] string? employeeId,
            [FromQuery] DateTime? date,
            [FromQuery] string? department)
        {
            var records = await _dataStore.GetAttendanceRecordsAsync(employeeId, date, department);
            return Ok(records);
        }

        [HttpGet("today/{employeeId}")]
        public async Task<ActionResult<AttendanceRecord?>> GetTodayAttendance(string employeeId)
        {
            var record = await _dataStore.GetTodayAttendanceAsync(employeeId);
            return Ok(record);
        }

        [HttpPost("clock-in")]
        public async Task<ActionResult<AttendanceRecord>> ClockIn([FromBody] ClockInRequest req)
        {
            try
            {
                var record = await _dataStore.ClockInAsync(req.EmployeeId, req.Notes);
                return Ok(record);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("clock-out")]
        public async Task<ActionResult<AttendanceRecord>> ClockOut([FromBody] ClockOutRequest req)
        {
            try
            {
                var record = await _dataStore.ClockOutAsync(req.EmployeeId, req.Notes);
                return Ok(record);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("manual")]
        public async Task<ActionResult<AttendanceRecord>> AddManual([FromBody] ManualAttendanceRequest req)
        {
            try
            {
                var record = await _dataStore.AddManualAttendanceAsync(req);
                return Ok(record);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

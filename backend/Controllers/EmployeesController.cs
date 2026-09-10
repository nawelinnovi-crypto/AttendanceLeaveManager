using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IDataStoreService _dataStore;

        public EmployeesController(IDataStoreService dataStore)
        {
            _dataStore = dataStore;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employee>>> GetEmployees()
        {
            var employees = await _dataStore.GetEmployeesAsync();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Employee>> GetEmployee(string id)
        {
            var emp = await _dataStore.GetEmployeeByIdAsync(id);
            if (emp == null) return NotFound(new { message = "Employee not found" });
            return Ok(emp);
        }

        [HttpGet("{id}/balance")]
        public async Task<ActionResult<LeaveBalance>> GetLeaveBalance(string id)
        {
            var balance = await _dataStore.GetLeaveBalanceAsync(id);
            return Ok(balance);
        }

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            if (employee == null) return BadRequest(new { message = "Employee data is required" });
            try
            {
                var created = await _dataStore.CreateEmployeeAsync(employee);
                return CreatedAtAction(nameof(GetEmployee), new { id = created.Id }, created);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Employee>> UpdateEmployee(string id, [FromBody] Employee employee)
        {
            if (employee == null) return BadRequest(new { message = "Employee data is required" });
            var updated = await _dataStore.UpdateEmployeeAsync(id, employee);
            if (updated == null) return NotFound(new { message = "Employee not found" });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEmployee(string id)
        {
            var result = await _dataStore.DeleteEmployeeAsync(id);
            if (!result) return NotFound(new { message = "Employee not found" });
            return NoContent();
        }
    }
}

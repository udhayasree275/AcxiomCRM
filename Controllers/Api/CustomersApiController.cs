using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersApiController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    private (string UserId, string UserEmail, string UserRole) GetUserContext()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        string role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        return (userId, userEmail, role);
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (userId, _, role) = GetUserContext();
        var (items, totalCount) = await _customerService.GetCustomersAsync(userId, role, search, status, null, page, pageSize);

        var dtos = items.Select(c => new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            CreatedDate = c.CreatedDate,
            OwnerName = c.Owner?.FullName
        });

        return Ok(new { total = totalCount, page, pageSize, data = dtos });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCustomerById(int id)
    {
        var (userId, _, role) = GetUserContext();
        var customer = await _customerService.GetByIdAsync(id, userId, role);

        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found or access denied." });
        }

        return Ok(new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            CreatedDate = customer.CreatedDate,
            OwnerName = customer.Owner?.FullName
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, userEmail, _) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Check uniqueness before create
        if (!await _customerService.IsEmailUniqueAsync(dto.Email))
        {
            return Conflict(new { message = "A customer with this email address already exists." });
        }

        if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
        {
            return Conflict(new { message = "A customer with this phone number already exists." });
        }

        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status ?? "Active",
            OwnerId = string.IsNullOrEmpty(dto.OwnerId) ? userId : dto.OwnerId
        };

        var (success, error, created) = await _customerService.CreateCustomerAsync(customer, userId, userEmail, ipAddress);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        var createdDto = new CustomerDto
        {
            CustomerId = created!.CustomerId,
            CustomerCode = created.CustomerCode,
            CustomerName = created.CustomerName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            Address = created.Address,
            City = created.City,
            State = created.State,
            Status = created.Status,
            CreatedDate = created.CreatedDate
        };

        return CreatedAtAction(nameof(GetCustomerById), new { id = created.CustomerId }, createdDto);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, userEmail, role) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var customer = new Customer
        {
            CustomerId = id,
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status ?? "Active",
            OwnerId = dto.OwnerId
        };

        var (success, error) = await _customerService.UpdateCustomerAsync(customer, userId, userEmail, role, ipAddress);
        if (!success)
        {
            if (error != null && error.Contains("not found")) return NotFound(new { message = error });
            if (error != null && error.Contains("already exists")) return Conflict(new { message = error });
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var (userId, userEmail, role) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _customerService.DeleteCustomerAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            if (error != null && error.Contains("not found")) return NotFound(new { message = error });
            return BadRequest(new { message = error });
        }

        return Ok(new { message = "Customer deleted or deactivated successfully." });
    }
}

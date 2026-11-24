using FluentValidation;
using Orders.BLL.DTOs.Customers;
using Orders.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Platform.Auth.Attributes;

namespace Orders.API.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController(ICustomerService service, IValidator<CreateCustomerRequest> validator) : ControllerBase
{
    [HttpGet]
    [RequirePermission("customers.read")]
    public async Task<ActionResult<IReadOnlyList<CustomerResponseDto>>> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    [RequirePermission("customers.read")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(int id, CancellationToken ct)
    {
        var result = await service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [RequirePermission("customers.write")]
    public async Task<ActionResult<CustomerResponseDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(request, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(
                validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())));
        }

        var result = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("customers.write")]
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        await service.UpdateAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("customers.write")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

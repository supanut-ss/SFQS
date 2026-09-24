using Freito.Api.Models;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Controllers;

[ApiController]
[Route("api/master")]
public sealed class MasterDataController(FreitoDbContext db) : ProtectedControllerBase
{
    [HttpGet("ports")]
    public async Task<IActionResult> GetPorts([FromQuery] PortType? type, CancellationToken cancellationToken)
    {
        var query = db.Ports.AsNoTracking();
        if (type is not null)
        {
            query = query.Where(x => x.Type == type);
        }

        return Ok(await query.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    [HttpGet("ports/{id:int}")]
    public async Task<IActionResult> GetPort(int id, CancellationToken cancellationToken)
    {
        var port = await db.Ports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return port is null ? NotFound() : Ok(port);
    }

    [HttpPost("ports")]
    public async Task<IActionResult> CreatePort(PortRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Code, request.Name, request.City, request.Country)) return BadRequest("Port fields cannot be blank.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Ports.AnyAsync(x => x.Code == code, cancellationToken)) return Conflict("A port with this code already exists.");

        var port = new Port
        {
            Code = code,
            Name = request.Name.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            Type = request.Type!.Value,
        };
        db.Ports.Add(port);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetPort), new { id = port.Id }, port);
    }

    [HttpPut("ports/{id:int}")]
    public async Task<IActionResult> UpdatePort(int id, PortRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Code, request.Name, request.City, request.Country)) return BadRequest("Port fields cannot be blank.");

        var port = await db.Ports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (port is null) return NotFound();
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Ports.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken)) return Conflict("A port with this code already exists.");
        if (port.Type != request.Type!.Value &&
            (await db.FreightRates.AnyAsync(x => x.OriginPortId == id || x.DestinationPortId == id, cancellationToken) ||
             await db.LocalCharges.AnyAsync(x => x.PortId == id, cancellationToken) ||
             await db.Quotations.AnyAsync(x => x.OriginPortId == id || x.DestinationPortId == id, cancellationToken)))
        {
            return Conflict("A port's type cannot change while it is referenced by a rate, charge, or quotation.");
        }

        port.Code = code;
        port.Name = request.Name.Trim();
        port.City = request.City.Trim();
        port.Country = request.Country.Trim();
        port.Type = request.Type!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(port);
    }

    [HttpDelete("ports/{id:int}")]
    public async Task<IActionResult> DeletePort(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var port = await db.Ports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (port is null) return NotFound();
        if (await db.FreightRates.AnyAsync(x => x.OriginPortId == id || x.DestinationPortId == id, cancellationToken) ||
            await db.LocalCharges.AnyAsync(x => x.PortId == id, cancellationToken) ||
            await db.Quotations.AnyAsync(x => x.OriginPortId == id || x.DestinationPortId == id, cancellationToken))
        {
            return Conflict("This port is referenced by rates, charges, or quotations and cannot be deleted.");
        }

        db.Ports.Remove(port);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("carriers")]
    public async Task<IActionResult> GetCarriers([FromQuery] CarrierType? type, CancellationToken cancellationToken)
    {
        var query = db.Carriers.AsNoTracking();
        if (type is not null) query = query.Where(x => x.Type == type);
        return Ok(await query.OrderBy(x => x.Name).ToListAsync(cancellationToken));
    }

    [HttpGet("carriers/{id:int}")]
    public async Task<IActionResult> GetCarrier(int id, CancellationToken cancellationToken)
    {
        var carrier = await db.Carriers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return carrier is null ? NotFound() : Ok(carrier);
    }

    [HttpPost("carriers")]
    public async Task<IActionResult> CreateCarrier(CarrierRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Code, request.Name)) return BadRequest("Carrier fields cannot be blank.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Carriers.AnyAsync(x => x.Code == code, cancellationToken)) return Conflict("A carrier with this code already exists.");

        var carrier = new Carrier { Code = code, Name = request.Name.Trim(), Type = request.Type!.Value };
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCarrier), new { id = carrier.Id }, carrier);
    }

    [HttpPut("carriers/{id:int}")]
    public async Task<IActionResult> UpdateCarrier(int id, CarrierRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Code, request.Name)) return BadRequest("Carrier fields cannot be blank.");
        var carrier = await db.Carriers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (carrier is null) return NotFound();
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Carriers.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken)) return Conflict("A carrier with this code already exists.");
        if (carrier.Type != request.Type!.Value && await db.FreightRates.AnyAsync(x => x.CarrierId == id, cancellationToken))
            return Conflict("A carrier's type cannot change while it is referenced by a freight rate.");

        carrier.Code = code;
        carrier.Name = request.Name.Trim();
        carrier.Type = request.Type!.Value;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(carrier);
    }

    [HttpDelete("carriers/{id:int}")]
    public async Task<IActionResult> DeleteCarrier(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var carrier = await db.Carriers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (carrier is null) return NotFound();
        if (await db.FreightRates.AnyAsync(x => x.CarrierId == id, cancellationToken)) return Conflict("This carrier is referenced by a freight rate and cannot be deleted.");
        db.Carriers.Remove(carrier);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("currencies")]
    public async Task<IActionResult> GetCurrencies(CancellationToken cancellationToken) =>
        Ok(await db.Currencies.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken));

    [HttpGet("currencies/{code}")]
    public async Task<IActionResult> GetCurrency(string code, CancellationToken cancellationToken)
    {
        var currency = await db.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code.ToUpper(), cancellationToken);
        return currency is null ? NotFound() : Ok(currency);
    }

    [HttpPost("currencies")]
    public async Task<IActionResult> CreateCurrency(CurrencyRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Code, request.Name)) return BadRequest("Currency fields cannot be blank.");
        var code = request.Code.Trim().ToUpperInvariant();
        if (await db.Currencies.AnyAsync(x => x.Code == code, cancellationToken)) return Conflict("A currency with this code already exists.");

        var currency = new Currency { Code = code, Name = request.Name.Trim(), DecimalDigits = request.DecimalDigits };
        db.Currencies.Add(currency);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCurrency), new { code = currency.Code }, currency);
    }

    [HttpPut("currencies/{code}")]
    public async Task<IActionResult> UpdateCurrency(string code, CurrencyRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (!normalizedCode.Equals(request.Code.Trim(), StringComparison.OrdinalIgnoreCase)) return BadRequest("Currency codes cannot be changed after creation.");
        if (!HasText(request.Name)) return BadRequest("Currency name cannot be blank.");
        var currency = await db.Currencies.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
        if (currency is null) return NotFound();
        currency.Name = request.Name.Trim();
        currency.DecimalDigits = request.DecimalDigits;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(currency);
    }

    [HttpDelete("currencies/{code}")]
    public async Task<IActionResult> DeleteCurrency(string code, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var normalizedCode = code.Trim().ToUpperInvariant();
        var currency = await db.Currencies.FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
        if (currency is null) return NotFound();
        if (await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == normalizedCode, cancellationToken) ||
            await db.FreightRates.AnyAsync(x => x.CurrencyCode == normalizedCode, cancellationToken) ||
            await db.LocalCharges.AnyAsync(x => x.CurrencyCode == normalizedCode, cancellationToken) ||
            await db.Quotations.AnyAsync(x => x.QuoteCurrency == normalizedCode, cancellationToken) ||
            await db.QuotationLines.AnyAsync(x => x.Currency == normalizedCode, cancellationToken))
        {
            return Conflict("This currency is referenced by rates, quotations, or exchange-rate history and cannot be deleted.");
        }

        db.Currencies.Remove(currency);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("exchange-rates")]
    public async Task<IActionResult> GetExchangeRates([FromQuery] string? currencyCode, CancellationToken cancellationToken)
    {
        var query = db.ExchangeRates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(currencyCode)) query = query.Where(x => x.CurrencyCode == currencyCode.Trim().ToUpper());
        return Ok(await query.OrderByDescending(x => x.EffectiveDate).ToListAsync(cancellationToken));
    }

    [HttpPost("exchange-rates")]
    public async Task<IActionResult> CreateExchangeRate(ExchangeRateRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (request.EffectiveDate == default) return BadRequest("An effective date is required.");
        var currencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        if (!await db.Currencies.AnyAsync(x => x.Code == currencyCode, cancellationToken)) return BadRequest("Currency does not exist.");
        if (currencyCode == "USD" && request.RateToBase != 1m) return BadRequest("The USD base-currency rate must remain 1.");

        var effectiveDate = UtcDate(request.EffectiveDate);
        if (await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == currencyCode && x.EffectiveDate == effectiveDate, cancellationToken))
        {
            return Conflict("An exchange rate already exists for this currency and effective date.");
        }

        var rate = new ExchangeRate { CurrencyCode = currencyCode, RateToBase = request.RateToBase, EffectiveDate = effectiveDate };
        db.ExchangeRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetExchangeRates), new { currencyCode }, rate);
    }

    [HttpGet("incoterms")]
    public async Task<IActionResult> GetIncoterms(CancellationToken cancellationToken) =>
        Ok(await db.Incoterms.AsNoTracking().OrderBy(x => x.Code).ToListAsync(cancellationToken));

    [HttpGet("incoterms/{code}/charge-rules")]
    public async Task<IActionResult> GetIncotermChargeRules(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        if (!await db.Incoterms.AnyAsync(x => x.Code == normalizedCode, cancellationToken)) return NotFound();
        return Ok(await db.IncotermChargeRules.AsNoTracking().Where(x => x.IncotermCode == normalizedCode).OrderBy(x => x.ChargeSide).ToListAsync(cancellationToken));
    }

    [HttpGet("cargo-types")]
    public async Task<IActionResult> GetCargoTypes(CancellationToken cancellationToken) =>
        Ok(await db.CargoTypes.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken));

    [HttpGet("cargo-types/{id:int}")]
    public async Task<IActionResult> GetCargoType(int id, CancellationToken cancellationToken)
    {
        var cargoType = await db.CargoTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return cargoType is null ? NotFound() : Ok(cargoType);
    }

    [HttpPost("cargo-types")]
    public async Task<IActionResult> CreateCargoType(CargoTypeRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Name)) return BadRequest("Cargo type name cannot be blank.");
        var name = request.Name.Trim();
        if (await db.CargoTypes.AnyAsync(x => x.Name == name, cancellationToken)) return Conflict("A cargo type with this name already exists.");
        var cargoType = new CargoType { Name = name, IsDangerous = request.IsDangerous, IsProhibited = request.IsProhibited };
        db.CargoTypes.Add(cargoType);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCargoType), new { id = cargoType.Id }, cargoType);
    }

    [HttpPut("cargo-types/{id:int}")]
    public async Task<IActionResult> UpdateCargoType(int id, CargoTypeRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (!HasText(request.Name)) return BadRequest("Cargo type name cannot be blank.");
        var cargoType = await db.CargoTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cargoType is null) return NotFound();
        var name = request.Name.Trim();
        if (await db.CargoTypes.AnyAsync(x => x.Id != id && x.Name == name, cancellationToken)) return Conflict("A cargo type with this name already exists.");
        cargoType.Name = name;
        cargoType.IsDangerous = request.IsDangerous;
        cargoType.IsProhibited = request.IsProhibited;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(cargoType);
    }

    [HttpDelete("cargo-types/{id:int}")]
    public async Task<IActionResult> DeleteCargoType(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var cargoType = await db.CargoTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (cargoType is null) return NotFound();
        if (await db.Quotations.AnyAsync(x => x.CargoTypeId == id, cancellationToken)) return Conflict("This cargo type is referenced by a quotation and cannot be deleted.");
        db.CargoTypes.Remove(cargoType);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var users = await db.Users.AsNoTracking().OrderBy(x => x.Email)
            .Select(x => new { x.Id, x.Email, x.Role, x.IsActive, x.CreatedAt }).ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var user = await db.Users.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.Email, x.Role, x.IsActive, x.CreatedAt }).FirstOrDefaultAsync(cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(UserRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken)) return Conflict("A user with this email already exists.");

        var user = new User
        {
            Email = email,
            PasswordHash = Auth.PasswordHasher.Hash(request.Password),
            Role = request.Role!.Value,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id, user.Email, user.Role, user.IsActive, user.CreatedAt });
    }

    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UserUpdateRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();

        user.Role = request.Role!.Value;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = Auth.PasswordHasher.Hash(request.Password);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.Email, user.Role, user.IsActive, user.CreatedAt });
    }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out _, UserRole.Admin);
        if (rejection is not null) return rejection;
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return NotFound();
        user.IsActive = false; // soft-deactivate — never hard delete, quotations reference CreatedByUserId/ApprovedByUserId
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool HasText(params string[] values) => values.All(value => !string.IsNullOrWhiteSpace(value));

    private static DateTime UtcDate(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
}

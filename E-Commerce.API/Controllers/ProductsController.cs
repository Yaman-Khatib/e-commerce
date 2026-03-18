using E_Commerce.Application.Products;
using E_Commerce.Application.Products.Models;
using E_Commerce.Application.ImportExport;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace E_Commerce_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService, IImportExportService importExportService) : ControllerBase
{
    private readonly IProductService _productService = productService;
    private readonly IImportExportService _importExportService = importExportService;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var products = await _productService.GetAllAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse>> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var created = await _productService.CreateAsync(userId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("import")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> ImportProducts([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "A non-empty CSV file is required." });
        }

        if (file.Length > int.MaxValue)
        {
            return BadRequest(new { error = "CSV file is too large." });
        }

        await using var stream = new MemoryStream(capacity: (int)file.Length);
        await file.CopyToAsync(stream, cancellationToken);
        stream.Position = 0;

        var result = await _importExportService.ImportProductsAsync(userId.Value, stream, cancellationToken);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpGet("export")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportProducts(CancellationToken cancellationToken)
    {
        var csv = await _importExportService.ExportProductsAsync(cancellationToken);
        return File(csv.Bytes, csv.ContentType, csv.FileName);
    }

    [HttpGet("sample")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportProductsSample(CancellationToken cancellationToken)
    {
        var csv = await _importExportService.ExportProductsSampleAsync(cancellationToken);
        return File(csv.Bytes, csv.ContentType, csv.FileName);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ProductResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}


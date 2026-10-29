using Microsoft.AspNetCore.Mvc;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController(StoreRepository repository) : ControllerBase
{
    /// <summary>Оформлення продажу через збережену процедуру dbo.sp_AddSale.</summary>
    /// <remarks>
    /// Процедура генерує користувацькі виключення:
    /// 50001 — кількість не більша за нуль; 50002 — товар не існує; 50003 — недостатньо товару на складі.
    /// Вони повертаються клієнту як 400 Bad Request з текстом помилки з бази даних.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateSaleRequest request)
    {
        var salesId = await repository.AddSaleAsync(request);
        return CreatedAtAction(nameof(GoodsController.GetById), "Goods", new { id = request.GoodId }, new { salesId });
    }
}

using Microsoft.AspNetCore.Mvc;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Route("api/goods")]
public class GoodsController(StoreRepository repository) : ControllerBase
{
    /// <summary>Список усіх товарів з назвою відділу.</summary>
    [HttpGet]
    public async Task<IEnumerable<Good>> GetAll() => await repository.GetGoodsAsync();

    /// <summary>Докладна інформація про товар та всі його продажі.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<GoodDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GoodDetails>> GetById(int id)
    {
        var details = await repository.GetGoodDetailsAsync(id);
        return details is null ? NotFound() : details;
    }
}

using Microsoft.AspNetCore.Mvc;
using StoreApi.Data;
using StoreApi.Models;

namespace StoreApi.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController(StoreRepository repository) : ControllerBase
{
    /// <summary>Список відділів.</summary>
    [HttpGet]
    public async Task<IEnumerable<Department>> GetAll() => await repository.GetDepartmentsAsync();

    /// <summary>Кількість товарів у відділі (скалярна функція dbo.fn_CountGoodsInDept).</summary>
    [HttpGet("{id:int}/goods-count")]
    public async Task<DeptGoodsCount> GetGoodsCount(int id) =>
        new(id, await repository.CountGoodsInDeptAsync(id));

    /// <summary>Товари відділу з ціною не нижче заданої (таблична функція dbo.fn_GetGoodsByDept).</summary>
    [HttpGet("{id:int}/goods")]
    public async Task<IEnumerable<DeptGood>> GetGoods(int id, [FromQuery] decimal minPrice = 0) =>
        await repository.GetGoodsByDeptAsync(id, minPrice);
}

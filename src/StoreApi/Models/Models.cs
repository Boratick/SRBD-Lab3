using System.ComponentModel.DataAnnotations;

namespace StoreApi.Models;

public record Department(int DeptId, string DepartmentName, string? Information);

public record Good(
    int GoodId,
    string Name,
    decimal? Price,
    int? Quantity,
    string? Producer,
    int? DeptId,
    string? DepartmentName,
    string? Description);

public record Sale(int SalesId, int CheckNo, int GoodId, int Quantity, DateTime? SaleDate);

/// <summary>Товар разом з усіма його продажами (дані з пов'язаних таблиць).</summary>
public record GoodDetails(Good Good, IReadOnlyList<Sale> Sales, int TotalSold);

/// <summary>Рядок результату табличної функції dbo.fn_GetGoodsByDept.</summary>
public record DeptGood(int GoodId, string Name, decimal? Price, int? Quantity, string? Producer, int SoldQuantity);

/// <summary>Результат скалярної функції dbo.fn_CountGoodsInDept.</summary>
public record DeptGoodsCount(int DeptId, int GoodsCount);

/// <summary>Параметри процедури dbo.sp_AddSale.</summary>
public class CreateSaleRequest
{
    [Required] public int GoodId { get; set; }
    [Required] public int Quantity { get; set; }
    [Required] public int CheckNo { get; set; }
}

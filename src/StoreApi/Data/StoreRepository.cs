using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using StoreApi.Models;

namespace StoreApi.Data;

public class StoreRepository(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("StoreDb")
        ?? throw new InvalidOperationException("Connection string 'StoreDb' is not configured.");

    private SqlConnection Open() => new(_connectionString);

    private const string GoodSelect = """
        SELECT g.GoodID AS GoodId, g.Name, g.Price, g.Quantity, g.Producer,
               g.DeptID AS DeptId, d.Department_Name AS DepartmentName, g.Description
        FROM dbo.Goods g
        LEFT JOIN dbo.Departments d ON d.DeptID = g.DeptID
        """;

    public async Task<IEnumerable<Department>> GetDepartmentsAsync()
    {
        await using var db = Open();
        return await db.QueryAsync<Department>(
            "SELECT DeptID AS DeptId, Department_Name AS DepartmentName, INFORMATION AS Information FROM dbo.Departments ORDER BY DeptID");
    }

    public async Task<IEnumerable<Good>> GetGoodsAsync()
    {
        await using var db = Open();
        return await db.QueryAsync<Good>(GoodSelect + " ORDER BY g.GoodID");
    }

    public async Task<GoodDetails?> GetGoodDetailsAsync(int id)
    {
        await using var db = Open();
        var good = await db.QuerySingleOrDefaultAsync<Good>(GoodSelect + " WHERE g.GoodID = @id", new { id });
        if (good is null) return null;

        var sales = (await db.QueryAsync<Sale>(
            """
            SELECT SalesID AS SalesId, Check_no AS CheckNo, GoodID AS GoodId, Quantity, SaleDate
            FROM dbo.Sales WHERE GoodID = @id ORDER BY SaleDate DESC, SalesID DESC
            """, new { id })).ToList();

        return new GoodDetails(good, sales, sales.Sum(s => s.Quantity));
    }

    public async Task<int> AddSaleAsync(CreateSaleRequest request)
    {
        await using var db = Open();
        var p = new DynamicParameters();
        p.Add("@GoodID", request.GoodId);
        p.Add("@Quantity", request.Quantity);
        p.Add("@CheckNo", request.CheckNo);
        p.Add("@NewSalesID", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await db.ExecuteAsync("dbo.sp_AddSale", p, commandType: CommandType.StoredProcedure);
        return p.Get<int>("@NewSalesID");
    }

    public async Task<int> CountGoodsInDeptAsync(int deptId)
    {
        await using var db = Open();
        return await db.ExecuteScalarAsync<int>("SELECT dbo.fn_CountGoodsInDept(@deptId)", new { deptId });
    }

    public async Task<IEnumerable<DeptGood>> GetGoodsByDeptAsync(int deptId, decimal minPrice)
    {
        await using var db = Open();
        return await db.QueryAsync<DeptGood>(
            """
            SELECT GoodID AS GoodId, Name, Price, Quantity, Producer, SoldQuantity
            FROM dbo.fn_GetGoodsByDept(@deptId, @minPrice) ORDER BY GoodID
            """, new { deptId, minPrice });
    }
}

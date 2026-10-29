-- Лабораторна робота №3: об'єкти бази даних для веб-застосунку
-- Виконується поверх бази Lab2_TriggersDB (таблиці Departments, Goods, Sales з ЛБ2)

USE Lab2_TriggersDB;
GO

-- Скалярна функція: кількість товарів у заданому відділі
CREATE OR ALTER FUNCTION dbo.fn_CountGoodsInDept
(
    @DeptID INT
)
RETURNS INT
AS
BEGIN
    DECLARE @Count INT;

    SELECT @Count = COUNT(*)
    FROM dbo.Goods
    WHERE DeptID = @DeptID;

    RETURN @Count;
END;
GO

-- Таблична функція: товари відділу з ціною не нижче заданої
CREATE OR ALTER FUNCTION dbo.fn_GetGoodsByDept
(
    @DeptID INT,
    @MinPrice DECIMAL(10,2)
)
RETURNS TABLE
AS
RETURN
(
    SELECT
        g.GoodID,
        g.Name,
        g.Price,
        g.Quantity,
        g.Producer,
        ISNULL(SUM(s.Quantity), 0) AS SoldQuantity
    FROM dbo.Goods g
    LEFT JOIN dbo.Sales s ON s.GoodID = g.GoodID
    WHERE g.DeptID = @DeptID
      AND g.Price >= @MinPrice
    GROUP BY g.GoodID, g.Name, g.Price, g.Quantity, g.Producer
);
GO

-- Процедура: оформлення продажу товару
-- Генерує користувацькі виключення (THROW 5000x), які обробляються у клієнтському застосунку
CREATE OR ALTER PROCEDURE dbo.sp_AddSale
    @GoodID INT,
    @Quantity INT,
    @CheckNo INT,
    @NewSalesID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @InStock INT;
    DECLARE @Msg NVARCHAR(255);

    IF @Quantity IS NULL OR @Quantity <= 0
        THROW 50001, N'Кількість товару у продажу має бути більшою за нуль.', 1;

    SELECT @InStock = Quantity
    FROM dbo.Goods
    WHERE GoodID = @GoodID;

    IF @@ROWCOUNT = 0
    BEGIN
        SET @Msg = CONCAT(N'Товар з кодом ', @GoodID, N' не існує.');
        THROW 50002, @Msg, 1;
    END;

    IF ISNULL(@InStock, 0) < @Quantity
    BEGIN
        SET @Msg = CONCAT(N'Недостатньо товару на складі: доступно ', ISNULL(@InStock, 0),
                          N' шт., запитано ', @Quantity, N' шт.');
        THROW 50003, @Msg, 1;
    END;

    BEGIN TRANSACTION;

    SELECT @NewSalesID = ISNULL(MAX(SalesID), 10249) + 1
    FROM dbo.Sales WITH (UPDLOCK, HOLDLOCK);

    INSERT INTO dbo.Sales (SalesID, Check_no, GoodID, Quantity, SaleDate)
    VALUES (@NewSalesID, @CheckNo, @GoodID, @Quantity, GETDATE());

    UPDATE dbo.Goods
    SET Quantity = Quantity - @Quantity
    WHERE GoodID = @GoodID;

    COMMIT TRANSACTION;
END;
GO

-- Додаткові тестові продажі, щоб сторінка товару мала що показати
IF NOT EXISTS (SELECT 1 FROM dbo.Sales WHERE SalesID = 10252)
BEGIN
    INSERT INTO dbo.Sales (SalesID, Check_no, GoodID, Quantity, SaleDate) VALUES
    (10252, 11113, 1, 1, '2026-10-20 10:15'),
    (10253, 11114, 3, 2, '2026-10-21 12:40'),
    (10254, 11115, 4, 3, '2026-10-22 16:05'),
    (10255, 11116, 5, 1, '2026-10-23 11:30');
END;
GO

-- Перевірка
SELECT dbo.fn_CountGoodsInDept(1) AS GoodsInDept1;
SELECT * FROM dbo.fn_GetGoodsByDept(3, 0);
GO

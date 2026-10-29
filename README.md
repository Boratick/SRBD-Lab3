# Лабораторна робота №3 — «Серверні рішення баз даних»

Веб-застосунок (back-end + front-end), побудований за підходом REST поверх бази даних
`Lab2_TriggersDB` (MS SQL Server), створеної на попередніх лабораторних роботах.

Виконав: Дрібниця Н.С., гр. ПЗПІ-24-6

## Технології

| Шар | Технологія |
|---|---|
| СУБД | Microsoft SQL Server 2022 Express, T-SQL (процедури, скалярні та табличні функції, `THROW`) |
| Back-end | ASP.NET Core Web API (.NET 10, C#), контролери REST |
| Доступ до даних | Microsoft.Data.SqlClient + Dapper |
| Документація API | Swagger / OpenAPI (Swashbuckle.AspNetCore) |
| Обробка помилок | `IExceptionHandler` + RFC 7807 Problem Details |
| Front-end | HTML5, CSS3, JavaScript (Fetch API), без фреймворків |

## Функціонал

1. **Виведення даних з пов'язаних таблиць**: список товарів (`Goods` + `Departments`), картка товару з усіма його продажами (`Sales`) на одній сторінці.
2. **Виконання процедури** `dbo.sp_AddSale(@GoodID, @Quantity, @CheckNo, @NewSalesID OUTPUT)`: оформлення продажу з форми застосунку.
3. **Скалярна функція** `dbo.fn_CountGoodsInDept(@DeptID)` (кількість товарів у відділі) і **таблична функція** `dbo.fn_GetGoodsByDept(@DeptID, @MinPrice)` (товари відділу з ціною від заданої та кількістю проданих).
4. **Оброблення виключень**: процедура генерує користувацькі помилки `THROW 50001/50002/50003`
   (некоректна кількість, товар не існує, недостатньо товару на складі). Сервер перехоплює `SqlException`
   і повертає `400 Bad Request` з текстом помилки, а клієнт показує його користувачу.

## REST API

| Метод | URL | Опис |
|---|---|---|
| GET | `/api/goods` | Усі товари з назвою відділу |
| GET | `/api/goods/{id}` | Товар + усі його продажі |
| POST | `/api/sales` | Продаж через `dbo.sp_AddSale` |
| GET | `/api/departments` | Список відділів |
| GET | `/api/departments/{id}/goods-count` | Скалярна функція `fn_CountGoodsInDept` |
| GET | `/api/departments/{id}/goods?minPrice=` | Таблична функція `fn_GetGoodsByDept` |

## Запуск

1. Виконати в SSMS скрипт [`database/lab3_objects.sql`](database/lab3_objects.sql) (потрібна база `Lab2_TriggersDB` з ЛБ2).
2. За потреби змінити рядок підключення `ConnectionStrings:StoreDb` у [`src/StoreApi/appsettings.json`](src/StoreApi/appsettings.json).
3. Запустити:
   ```
   cd src/StoreApi
   dotnet run
   ```
4. Відкрити http://localhost:5069 (веб-застосунок) або http://localhost:5069/swagger (Swagger UI).

## Структура

```
database/lab3_objects.sql          — процедура, функції, тестові дані
src/StoreApi/
  Controllers/                     — REST-контролери (Goods, Sales, Departments)
  Data/StoreRepository.cs          — виклики SQL, процедури та функцій
  Infrastructure/SqlExceptionHandler.cs — перетворення помилок SQL Server у HTTP-відповіді
  Models/Models.cs                 — моделі даних (DTO)
  wwwroot/                         — front-end (index.html, js/app.js, css/site.css)
```

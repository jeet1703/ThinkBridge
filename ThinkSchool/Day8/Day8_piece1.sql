CREATE TABLE Day8Quotes (
    Id INT NOT NULL,
    Author NVARCHAR(200) NOT NULL,
    Category NVARCHAR(200) NOT NULL,
    Text NVARCHAR(max) NOT NULL
);
GO

SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

WITH cnt(x) AS (
    SELECT 1
    UNION ALL
    SELECT x + 1 FROM cnt WHERE x < 100000
)
INSERT INTO Day8Quotes (Id, Author, Category, Text)
SELECT 
    x,
    'Author_' + CAST((x % 500) AS VARCHAR(10)),
    'Category_' + CAST((x % 20) AS VARCHAR(10)),
    'This is quote text number ' + CAST(x AS VARCHAR(10))
FROM cnt
OPTION (MAXRECURSION 0);
GO

SELECT * FROM Day8Quotes WHERE Id = 54321;
GO

SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

SELECT * FROM Day8Quotes WHERE Category = 'Category_10';
GO

CREATE CLUSTERED INDEX IX_Day8Quotes_Id ON Day8Quotes(Id);
GO

SELECT * FROM Day8Quotes WHERE Id = 54321;
GO

SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

CREATE NONCLUSTERED INDEX IX_Day8Quotes_Author ON Day8Quotes(Author);
CREATE NONCLUSTERED INDEX IX_Day8Quotes_Category ON Day8Quotes(Category);
GO

SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

SELECT * FROM Day8Quotes WHERE Category = 'Category_10';
GO

WITH cnt_write(x) AS (
    SELECT 100001
    UNION ALL
    SELECT x + 1 FROM cnt_write WHERE x < 120000
)
INSERT INTO Day8Quotes (Id, Author, Category, Text)
SELECT 
    x,
    'Author_' + CAST((x % 500) AS VARCHAR(10)),
    'Category_' + CAST((x % 20) AS VARCHAR(10)),
    'This is additional quote text number ' + CAST(x AS VARCHAR(10))
FROM cnt_write
OPTION (MAXRECURSION 0);
GO

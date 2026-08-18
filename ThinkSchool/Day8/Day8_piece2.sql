USE Day8IndexDb;
GO

SET STATISTICS IO ON;
GO

SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

DROP INDEX IX_Day8Quotes_Author ON Day8Quotes;
GO

CREATE NONCLUSTERED INDEX IX_Day8Quotes_Author ON Day8Quotes(Author) INCLUDE(Category, Text);
GO

SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

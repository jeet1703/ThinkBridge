// Query 1 

SELECT * FROM Day8Quotes WHERE Id = 54321;

Result Set Batch 1 - Query 1
========================================

Id          Author      Category    Text                           
----------  ----------  ----------  -------------------------------
54321       Author_321  Category_1  This is quote text number 54321
(1 row affected)


11:36:14 AM
Started executing query at  Line 1
SQL Server parse and compile time: 
   CPU time = 0 ms, elapsed time = 0 ms.
SQL Server parse and compile time: 
   CPU time = 0 ms, elapsed time = 0 ms.
(1 row affected)
Table 'Day8Quotes'. Scan count 1, logical reads 3, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.
 SQL Server Execution Times:
   CPU time = 0 ms,  elapsed time = 0 ms.
11:36:14 AM
Total execution time: 00:00:00.014



//Query 2


SELECT * FROM Day8Quotes WHERE Author = 'Author_250';
GO

11:40:23 AM
Started executing query at  Line 1
SQL Server parse and compile time: 
   CPU time = 152 ms, elapsed time = 152 ms.
SQL Server parse and compile time: 
   CPU time = 0 ms, elapsed time = 0 ms.
(240 rows affected)
Table 'Day8Quotes'. Scan count 1, logical reads 726, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.
 SQL Server Execution Times:
   CPU time = 0 ms,  elapsed time = 3 ms.
11:40:23 AM
Total execution time: 00:00:00.167



//Query 3 
SELECT * FROM Day8Quotes WHERE Category = 'Category_10';

11:41:52 AM
Started executing query at  Line 1
SQL Server parse and compile time: 
   CPU time = 2 ms, elapsed time = 2 ms.
SQL Server parse and compile time: 
   CPU time = 0 ms, elapsed time = 0 ms.
(6000 rows affected)
Table 'Day8Quotes'. Scan count 1, logical reads 1950, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob page server reads 0, lob read-ahead reads 0, lob page server read-ahead reads 0.
 SQL Server Execution Times:
   CPU time = 40 ms,  elapsed time = 40 ms.
11:41:52 AM
Total execution time: 00:00:00.087


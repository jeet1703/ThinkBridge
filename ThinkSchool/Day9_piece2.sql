USE Day8IndexDb;
GO

-- 1. REPRO
BEGIN TRAN;
UPDATE Day8Quotes SET Author = 'Author_A' WHERE Id = 54321;
UPDATE Day8Quotes SET Author = 'Author_B' WHERE Id = 54322;
ROLLBACK TRAN;

BEGIN TRAN;
UPDATE Day8Quotes SET Author = 'Author_B' WHERE Id = 54322;
UPDATE Day8Quotes SET Author = 'Author_A' WHERE Id = 54321;
ROLLBACK TRAN;

-- 2. VICTIM MESSAGE

-- 3. FIX
BEGIN TRAN;
UPDATE Day8Quotes SET Author = 'Author_A' WHERE Id = 54321;
UPDATE Day8Quotes SET Author = 'Author_B' WHERE Id = 54322;
COMMIT TRAN;

BEGIN TRAN;
UPDATE Day8Quotes SET Author = 'Author_A' WHERE Id = 54321;
UPDATE Day8Quotes SET Author = 'Author_B' WHERE Id = 54322;
COMMIT TRAN;

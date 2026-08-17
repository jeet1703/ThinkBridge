// query 1 

SELECT Author FROM Quotes
EXCEPT
SELECT DISTINCT q.Author
FROM Quotes q
JOIN QuoteTags qt ON q.Id = qt.QuoteId;

// result 1 
Author
---------------
Ada Lovelace   
Alan Turing    
Richard Feynman
Stephen Hawking
Test Author    
sqlite> 

// Operator used: EXCEPT. Rationale: EXCEPT was used to subtract authors of tagged quotes from all authors, leaving only authors with quotes but no tags.

//query 2 

SELECT DISTINCT q.Author
FROM Quotes q
JOIN CollectionItem ci ON q.Id = ci.QuoteId
JOIN Collections c ON ci.CollectionId = c.Id
WHERE c.Name = 'classic'
INTERSECT
SELECT DISTINCT q.Author
FROM Quotes q
JOIN CollectionItem ci ON q.Id = ci.QuoteId
JOIN Collections c ON ci.CollectionId = c.Id
WHERE c.Name = 'modern';


//result 2
Author      
------------
Carl Sagan  
Nikola Tesla

// Operator used: INTERSECT. Rationale: INTERSECT was used to find only those authors who have quotes present in both the classic and modern sets.

//query 3 
SELECT Name FROM Tags WHERE Category = 'philosophy'
UNION
SELECT Name FROM Tags WHERE Category = 'science';


//result 3
Name
---------
cosmos
discovery
gravity
life
mind
truth
wisdom
sqlite>

// Operator used: UNION. Rationale: UNION was used to combine distinct tag names from two categories, merging duplicates (like the tag 'truth') into a single distinct set.



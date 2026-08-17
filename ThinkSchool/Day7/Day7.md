WITH AuthorStats AS (
    SELECT 
        Author, 
        COUNT(*) AS QuoteCount, 
        MAX(Id) AS MostRecentQuoteId
    FROM Quotes
    GROUP BY Author
)
SELECT 
    stats.Author,
    stats.QuoteCount,
    q.Text AS MostRecentQuote
FROM AuthorStats stats
JOIN Quotes q ON q.Id = stats.MostRecentQuoteId;

-- Results:
Author           QuoteCount  MostRecentQuote
---------------  ----------  ------------------------------------------------------------
Ada Lovelace     1           That brain of mine is something more than merely mortal; as
                             time will show.

Alan Turing      1           We can only see a short distance ahead, but we can see plent
                             y there that needs to be done.

Albert Einstein  3           Imagination is more important than knowledge.

Carl Sagan       2           We are a way for the cosmos to know itself.

Galileo Galilei  2           All truths are easy to understand once they are discovered;
                             the point is to discover them.

Isaac Newton     2           My most recent quote: Gravity explains the motions of the pl
                             anets, but it cannot explain who sets the planets in motion.

Marie Curie      2           Be less curious about people and more curious about ideas.

Nikola Tesla     1           The present is theirs; the future, for which I really worked
                             , is mine.

Richard Feynman  1           The first principle is that you must not fool yourself and y
                             ou are the easiest person to fool.

Stephen Hawking  2           However difficult life may seem, there is always something y
                             ou can do and succeed at.
sqlite>

Why a CTE here over a correlated subquery?
A CTE groups and finds the latest IDs in one pass to perform an efficient join. On the other hand a correlated subquery executes sequentially for every outer row, leading to severe N+1 performance degradation.

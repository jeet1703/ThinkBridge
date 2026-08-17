SELECT 
    Author,
    Text,
    CreatedAt,
    ROW_NUMBER() OVER (PARTITION BY Author ORDER BY CreatedAt) AS RunningCount,
    ROUND(julianday(CreatedAt) - julianday(LAG(CreatedAt) OVER (PARTITION BY Author ORDER BY CreatedAt))) AS DaysSincePrevious
FROM Quotes
ORDER BY Author, CreatedAt;

-- Results:
-- Author           Text                                                                                                                         CreatedAt            RunningCount  DaysSincePrevious
-- ---------------  ---------------------------------------------------------------------------------------------------------------------------  -------------------  ------------  -----------------
-- Ada Lovelace     That brain of mine is something more than merely mortal; as time will show.                                                  2026-08-01 10:00:00  1             
-- Alan Turing      We can only see a short distance ahead, but we can see plenty there that needs to be done.                                   2026-08-01 10:00:00  1             
-- Albert Einstein  Imagination is more important than knowledge.                                                                                2026-08-01 10:00:00  1             
-- Albert Einstein  Imagination is more important than knowledge.                                                                                2026-08-03 12:00:00  2             2.0
-- Albert Einstein  Imagination is more important than knowledge.                                                                                2026-08-07 15:30:00  3             4.0
-- Carl Sagan       Somewhere, something incredible is waiting to be known.                                                                      2026-08-01 10:00:00  1             
-- Carl Sagan       We are a way for the cosmos to know itself.                                                                                  2026-08-03 14:00:00  2             2.0
-- Galileo Galilei  You cannot teach a man anything; you can only help him find it within himself.                                               2026-08-01 10:00:00  1             
-- Galileo Galilei  All truths are easy to understand once they are discovered; the point is to discover them.                                   2026-08-09 11:00:00  2             8.0
-- Isaac Newton     If I have seen further it is by standing on the shoulders of Giants.                                                         2026-08-01 09:00:00  1             
-- Isaac Newton     My most recent quote: Gravity explains the motions of the planets, but it cannot explain who sets the planets in motion.     2026-08-05 14:00:00  2             4.0
-- Marie Curie      Nothing in life is to be feared, it is only to be understood. Now is the time to understand more, so that we may fear less.  2026-08-01 11:00:00  1             
-- Marie Curie      Be less curious about people and more curious about ideas.                                                                   2026-08-04 16:30:00  2             3.0
-- Nikola Tesla     The present is theirs; the future, for which I really worked, is mine.                                                       2026-08-01 10:00:00  1             
-- Richard Feynman  The first principle is that you must not fool yourself and you are the easiest person to fool.                               2026-08-01 10:00:00  1             
-- Stephen Hawking  Quiet people have the loudest minds.                                                                                         2026-08-01 09:00:00  1             
-- Stephen Hawking  However difficult life may seem, there is always something you can do and succeed at.                                        2026-08-06 13:00:00  2             5.0
-- Test Author      This should fail                                                                                                             2026-08-01 08:00:00  1             
-- Test Author      This should fail                                                                                                             2026-08-02 08:00:00  2             1.0
-- Test Author      This should fail                                                                                                             2026-08-05 08:00:00  3             3.0
-- Test Author      This should fail                                                                                                             2026-08-07 08:00:00  4             2.0
-- Test Author      This should fail                                                                                                             2026-08-10 08:00:00  5             3.0
-- Test Author      This should fail                                                                                                             2026-08-15 08:00:00  6             5.0
-- Test Author      This should fail                                                                                                             2026-08-20 08:00:00  7             5.0

-- What did you learn this session?
-- Window functions (ROW_NUMBER and LAG) allow us to compute running counts and historical row comparisons (like date differences) in a single SELECT pass without complex self-joins or temporary tables.

-- What would break this?
-- If two quotes by the same author have the exact same CreatedAt value, the order in the window function is non-deterministic, which could result in incorrect or zero day-gap calculations. A secondary sorting key like Id is needed to guarantee deterministic results.

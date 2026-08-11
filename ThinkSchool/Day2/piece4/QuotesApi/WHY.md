A rich model keeps the important business rules inside the entity instead of expecting every part of the application to handel them correctly. This makes the mmodel safer and easier to work with

with the rich Quote model, rules like the maximum author and text length are handled inside Quote.Create(). Any part of the application creating a quote has to go through the same validation. This avoids having the same checks repeated across controllers, services, or background jobs.

Another benefit is that important properties can be protected from being changed directly after creation. This helps keep the entity in a valid state throughout its lifetime.

Overall the rich model puts the rules where they belong and makes it hard for the other part of the application to accidently create invalid data.
import { Component, signal } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { AuthorsListComponent } from './authors-list/authors-list.component';
import { QuotesExplorerComponent } from './quotes-explorer/quotes-explorer.component';
import { CreateQuoteFormComponent } from './create-quote-form/create-quote-form.component';
import { CreateQuoteFormSignalsComponent } from './create-quote-form-signals/create-quote-form-signals.component';

type Tab = 'quotes' | 'authors' | 'explorer' | 'add' | 'add-signals';

@Component({
  selector: 'app-root',
  imports: [
    QuotesListComponent,
    AuthorsListComponent,
    QuotesExplorerComponent,
    CreateQuoteFormComponent,
    CreateQuoteFormSignalsComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('quotes-ui');
  protected readonly activeTab = signal<Tab>('quotes');

  protected selectTab(tab: Tab): void {
    this.activeTab.set(tab);
  }
}

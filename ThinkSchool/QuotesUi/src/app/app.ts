import { Component, signal } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { AuthorsListComponent } from './authors-list/authors-list.component';
import { QuotesExplorerComponent } from './quotes-explorer/quotes-explorer.component';
import { CreateQuoteFormComponent } from './create-quote-form/create-quote-form.component';

type Tab = 'quotes' | 'authors' | 'explorer' | 'add';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent, AuthorsListComponent, QuotesExplorerComponent, CreateQuoteFormComponent],
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

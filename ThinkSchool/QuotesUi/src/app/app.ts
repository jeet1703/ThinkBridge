import { Component, signal } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';
import { AuthorsListComponent } from './authors-list/authors-list.component';

type Tab = 'quotes' | 'authors';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent, AuthorsListComponent],
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

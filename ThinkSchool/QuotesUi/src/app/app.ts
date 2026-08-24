import { Component, signal } from '@angular/core';
import { QuotesListComponent } from './quotes-list/quotes-list.component';

@Component({
  selector: 'app-root',
  imports: [QuotesListComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('quotes-ui');
}
